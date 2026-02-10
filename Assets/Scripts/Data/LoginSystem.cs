using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Google;
using System;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;



public class LoginSystem : MonoBehaviour
{
    [Header("로그인 시스템")]
    [SerializeField] TMP_InputField emailInputField;
    [SerializeField] TMP_InputField passwordInputField;
    [SerializeField] TMP_InputField nickNameInputField;
    [SerializeField] Button loginBtn;
    [SerializeField] Button registerBtn;
    [SerializeField] Button googleLoginBtn;
    [SerializeField] TextMeshProUGUI resultText;

    [Header("닉네임 설정 시스템")]
    [SerializeField] GameObject nickNameInputPanel;
    [SerializeField] TMP_InputField newNickNameInputField;
    [SerializeField] Button nickNameConfirmBtn;

    private void Start()
    {
        Init();
    }

    private void Init()
    {
        loginBtn.onClick.AddListener(() => { StartCoroutine(LoginCoroutine(emailInputField.text, passwordInputField.text )); });
        registerBtn.onClick.AddListener(() => { StartCoroutine(RegisterCoroutine(emailInputField.text, passwordInputField.text, nickNameInputField.text)); });
        googleLoginBtn.onClick.AddListener(() => GoogleLogin());
        nickNameConfirmBtn.onClick.AddListener(() => SetPlayerName(newNickNameInputField.text));
    }

    #region 파이어베이스 로그인 시스템
    IEnumerator LoginCoroutine(string email, string password)
    {

        Task<AuthResult> LoginTask = FirebaseAuthManager.Instance.auth.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(predicate: () => LoginTask.IsCompleted);
        if (LoginTask.Exception != null) // 로그인에서 문제가 발생했을 때 Exception에 담김
        {
            Debug.Log("다음과 같은 이유로 로그인 실패" + LoginTask.Exception);
            //파이어베이스에선, 에러를 파이어베이스 형식으로 해석할 수 있게 클래스 제공
            FirebaseException firebaseEx = LoginTask.Exception.GetBaseException() as FirebaseException;
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;//진짜 우리가 해석 가능한 형태로 바꿈
            string message = "";
            switch (errorCode)
            {
                case AuthError.MissingEmail:
                    message = "Missing Email";
                    break;
                case AuthError.MissingPassword:
                    message = "MissingPassword";
                    break;
                case AuthError.WrongPassword:
                    message = "WrongPassword";
                    break;
                case AuthError.InvalidEmail:
                    message = "InvalidEmail";
                    break;
                case AuthError.UserNotFound:
                    message = "UserNotFound";
                    break;
                default:
                    message = "IDONTKNOW";
                    break;
            }
            Debug.Log(message);
        }
        else//여기 왔단 뜻은 성공
        {
            FirebaseAuthManager.Instance.user = LoginTask.Result.User;//로그인 잘 되었으니, 유저 정보 기억
            nickNameInputField.text = FirebaseAuthManager.Instance.user.DisplayName;//파이어베이스 상에 기억된 닉네임 가져옴
            //loginButton.interactable = true;
            Debug.Log("로그인 성공");
            SceneManager.LoadSceneAsync("Lobby");
        }
    }
    #endregion
    #region 파이어베이스 회원가입 시스템

    bool CheckSetting()
    {
        // 이메일 형식이 최소한의 틀을 갖췄을 때만 체크 시작
        if (!emailInputField.text.Contains("@") || !emailInputField.text.Contains("."))
        {
            Debug.Log("이메일 양식 불일치");
            return false;
        }
        if (passwordInputField.text.Length < 9)
        {
            Debug.Log("비밀번호는 9자리 이상");
            return false;
        }
        if (nickNameInputField.text.Length <4)
        {
            Debug.Log("닉네임은 4글자 이상");
            return false;
        }
        return true;
    }
    IEnumerator RegisterCoroutine(string email, string password, string UserName)
    {
        //일단 양식 지키는지 확인
        if (!CheckSetting())
        {
            yield break;
        }

        Task<AuthResult> RegisterTask = FirebaseAuthManager.Instance.auth.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(predicate: () => RegisterTask.IsCompleted);
        if (RegisterTask.Exception != null)
        {
            Debug.LogWarning(message: "실패 사유" + RegisterTask.Exception);
            FirebaseException firebaseEx = RegisterTask.Exception.GetBaseException() as FirebaseException;
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

            string message = "회원가입 실패";
            switch (errorCode)
            {
                case AuthError.MissingEmail:
                    message = "이메일 누락";
                    break;
                case AuthError.MissingPassword:
                    message = "패스워드 누락";
                    break;
                case AuthError.WeakPassword:
                    message = "패스워드 약함";
                    break;
                case AuthError.EmailAlreadyInUse:
                    message = "중복 이메일";
                    break;
                default:
                    message = "기타 사유. 관리자 문의 바람";
                    break;
            }
            Debug.Log(message);
        }
        else//생성 완료
        {
            FirebaseAuthManager.Instance.user = RegisterTask.Result.User;
            if (FirebaseAuthManager.Instance.user != null)
            {

                //이건 로컬에서 만든 것
                UserProfile profile = new UserProfile { DisplayName = UserName };

                Task profileTask = FirebaseAuthManager.Instance.user.UpdateUserProfileAsync(profile);
                //predicate : 참거짓을 판단하는 함수에 저걸 넣겠다
                yield return new WaitUntil(predicate: () => profileTask.IsCompleted);
                //여기서 닉네임에 욕 들어가면 차단하도록
                if (profileTask.Exception != null)
                {
                    Debug.LogWarning("닉네임 설정 실패 " + profileTask.Exception);
                    FirebaseException firebaseEx = profileTask.Exception.GetBaseException() as FirebaseException;
                    AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
                }
                else
                {
                    //loginButton.interactable = true;
                }
            }
            Debug.Log("회원가입 성공 " + FirebaseAuthManager.Instance.user.DisplayName + "님 환영합니다");
        }

    }
    #endregion



    #region 구글 로그인 및 회원가입 시스템
    private void GoogleLogin()
    {
        try
        {
            GoogleSignIn.Configuration = new GoogleSignInConfiguration()
            {
                WebClientId = "249565570939-308ohhucvvejunq0bo4msrimbdrehh6k.apps.googleusercontent.com",
                RequestIdToken = true,
                UseGameSignIn = false,
                RequestEmail = true,
            };

            GoogleSignIn.DefaultInstance.SignIn().ContinueWith(task =>
            {
                if (task.IsFaulted)
                    Debug.LogError("로그인 실패: " + task.Exception);
                else if (task.IsCanceled)
                {
                    Debug.LogError("로그인 취소: " + task.IsCanceled);
                }
                else
                {
                    //앞선 문제점들 모두 지나면 정상적으로 로그인 되었다는 뜻
                    OnGoogleAuthenticatedFinished(task);
                }
            });
        }
        catch (Exception err)
        {
            Debug.LogError("로그인 작업 에러 발생 : "+ err.Message);
        }
    }

    //전달받은 구글 데이터 기반으로 파이어 베이스 로그인
    private void OnGoogleAuthenticatedFinished(Task<GoogleSignInUser> task)
    {

        Credential credential = GoogleAuthProvider.GetCredential(task.Result.IdToken, null);

        FirebaseAuthManager.Instance.auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(loginTask =>
        {
            if (loginTask.IsCanceled) { return; }

            if (loginTask.IsFaulted)
            {
                Debug.LogError($"SignInWithCredentialAsync encountered an error: {loginTask.Exception}");
                return;
            }
            FirebaseAuthManager.Instance.user = FirebaseAuthManager.Instance.auth.CurrentUser;


            if (string.IsNullOrEmpty(FirebaseAuthManager.Instance.user.DisplayName))
            {
                SetPlayerNameSequence();
            }
            //Debug.Log($"UserName: {user.DisplayName}");
            //Debug.Log($"UserEmail: {user.Email}");

            //userIdTMP.text = $"Google UserId: {user.UserId}";
            //userNameTMP.text = $"User Name: {user.DisplayName}";
        });
    }

    private void SetPlayerNameSequence()
    {
        nickNameInputPanel.SetActive(true);
    }

    private void SetPlayerName(string playerName)
    {
        //이건 로컬에서 만든 것
        UserProfile profile = new UserProfile { DisplayName = playerName };

        Task profileTask = FirebaseAuthManager.Instance.user.UpdateUserProfileAsync(profile);
    }
    #endregion
}
