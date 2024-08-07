﻿using BellyCare.Views;

namespace BellyCare.Shells
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(PatientProfileView), typeof(PatientProfileView));
            Routing.RegisterRoute(nameof(PatientHomeView), typeof(PatientHomeView));
        }
    }
}
