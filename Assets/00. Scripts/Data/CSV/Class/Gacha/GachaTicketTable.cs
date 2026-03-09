using System;
using System.Collections.Generic;

[System.Serializable]
public class GachaTicketTable : TableBase
{
    public GachaType ticketType;
    public int price;
    public string ticketResources;
    public string ticketName;
}
