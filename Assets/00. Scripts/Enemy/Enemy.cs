using UnityEngine;

public class Enemy : MonoBehaviour
{
    private bool Isdead;
    private EnemyStateMachine fsm;
    
    [SerializeField] private Collider enemyCollider;

    public Collider EnemyCollider => enemyCollider;
    private void Awake()
    {
        fsm = transform.GetComponent<EnemyStateMachine>();
    }

    void Start()
    {
        if (enemyCollider == null)
        {
            enemyCollider = GetComponent<Collider>();
        }
    }

    private void OnEnable()
    {
        EnemyRegistry.Register(this);
    }

    private void OnDisable()
    {
        EnemyRegistry.UnRegister(this);
    }

    private void OnDestroy()
    {
        EnemyRegistry.UnRegister(this);
    }

    private void Update()
    {
        if (fsm.CurrentStateType == EnemyStateType.Dead && !Isdead)
        {
            EnemyRegistry.UnRegister(this);
            Isdead = true;

            //transform.GetComponent<Renderer>().material.color = Color.black;
        }
    }
}
