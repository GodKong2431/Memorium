using System;
using System.Collections.Generic;

[System.Serializable]
public class GachaTicketTable : TableBase
{
    public TicketType ticketType;
    public int price;
    public string ticketResources;
    public string ticketName;
}
