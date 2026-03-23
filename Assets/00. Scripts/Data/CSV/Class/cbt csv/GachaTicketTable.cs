using System;
using System.Collections.Generic;

[System.Serializable]
public class GachaTicketTable : TableBase
{
    public string ticketTypeDesc;
    public TicketType ticketType;
    public int price;
    public string ticketName;
    public string ticketResources;
}
