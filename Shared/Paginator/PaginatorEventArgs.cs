namespace WhatToEatApp.Shared.Paginator
{
    public class PaginatorEventArgs : EventArgs
    {
        public int Skip { get; set; }
        public int Take { get; set; }
    }
}