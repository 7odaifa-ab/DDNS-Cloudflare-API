﻿#pragma checksum "..\..\..\..\..\Views\Pages\DashboardPage.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "1127A006330AB8C1BAF085031741B184C6549FE3"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using DDNS_Cloudflare_API.Views.Pages;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Converters;
using Wpf.Ui.Markup;


namespace DDNS_Cloudflare_API.Views.Pages {
    
    
    /// <summary>
    /// DashboardPage
    /// </summary>
    public partial class DashboardPage : System.Windows.Controls.Page, System.Windows.Markup.IComponentConnector {
        
        
        #line 22 "..\..\..\..\..\Views\Pages\DashboardPage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal Wpf.Ui.Controls.TextBox txtApiKey;
        
        #line default
        #line hidden
        
        
        #line 25 "..\..\..\..\..\Views\Pages\DashboardPage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal Wpf.Ui.Controls.TextBox txtZoneId;
        
        #line default
        #line hidden
        
        
        #line 28 "..\..\..\..\..\Views\Pages\DashboardPage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal Wpf.Ui.Controls.TextBox txtDnsRecordId;
        
        #line default
        #line hidden
        
        
        #line 31 "..\..\..\..\..\Views\Pages\DashboardPage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal Wpf.Ui.Controls.TextBox txtName;
        
        #line default
        #line hidden
        
        
        #line 34 "..\..\..\..\..\Views\Pages\DashboardPage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox cmbType;
        
        #line default
        #line hidden
        
        
        #line 41 "..\..\..\..\..\Views\Pages\DashboardPage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox cmbTtl;
        
        #line default
        #line hidden
        
        
        #line 57 "..\..\..\..\..\Views\Pages\DashboardPage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox cmbInterval;
        
        #line default
        #line hidden
        
        
        #line 67 "..\..\..\..\..\Views\Pages\DashboardPage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal Wpf.Ui.Controls.Button btnStart;
        
        #line default
        #line hidden
        
        
        #line 71 "..\..\..\..\..\Views\Pages\DashboardPage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal Wpf.Ui.Controls.Button btnStop;
        
        #line default
        #line hidden
        
        
        #line 75 "..\..\..\..\..\Views\Pages\DashboardPage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal Wpf.Ui.Controls.Button btnOneTime;
        
        #line default
        #line hidden
        
        
        #line 79 "..\..\..\..\..\Views\Pages\DashboardPage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock txtStatus;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "8.0.7.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/DDNS_Cloudflare_API;component/views/pages/dashboardpage.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\..\Views\Pages\DashboardPage.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "8.0.7.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.txtApiKey = ((Wpf.Ui.Controls.TextBox)(target));
            return;
            case 2:
            this.txtZoneId = ((Wpf.Ui.Controls.TextBox)(target));
            return;
            case 3:
            this.txtDnsRecordId = ((Wpf.Ui.Controls.TextBox)(target));
            return;
            case 4:
            this.txtName = ((Wpf.Ui.Controls.TextBox)(target));
            return;
            case 5:
            this.cmbType = ((System.Windows.Controls.ComboBox)(target));
            return;
            case 6:
            this.cmbTtl = ((System.Windows.Controls.ComboBox)(target));
            return;
            case 7:
            this.cmbInterval = ((System.Windows.Controls.ComboBox)(target));
            return;
            case 8:
            this.btnStart = ((Wpf.Ui.Controls.Button)(target));
            
            #line 69 "..\..\..\..\..\Views\Pages\DashboardPage.xaml"
            this.btnStart.Click += new System.Windows.RoutedEventHandler(this.BtnStart_Click);
            
            #line default
            #line hidden
            return;
            case 9:
            this.btnStop = ((Wpf.Ui.Controls.Button)(target));
            
            #line 73 "..\..\..\..\..\Views\Pages\DashboardPage.xaml"
            this.btnStop.Click += new System.Windows.RoutedEventHandler(this.BtnStop_Click);
            
            #line default
            #line hidden
            return;
            case 10:
            this.btnOneTime = ((Wpf.Ui.Controls.Button)(target));
            
            #line 77 "..\..\..\..\..\Views\Pages\DashboardPage.xaml"
            this.btnOneTime.Click += new System.Windows.RoutedEventHandler(this.BtnOneTime_Click);
            
            #line default
            #line hidden
            return;
            case 11:
            this.txtStatus = ((System.Windows.Controls.TextBlock)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}

