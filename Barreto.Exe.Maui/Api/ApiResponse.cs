﻿namespace Barreto.Exe.Maui.Api
{
    public class ApiResponse<T>
    {
        public T Data { get; set; }
        public string Message { get; set; }
    }
}
