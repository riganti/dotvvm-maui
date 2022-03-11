using DotVVM.Framework.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotVVM.Maui.App.HostedApp.ViewModels
{
    public class DefaultViewModel : DotvvmViewModelBase
    {

        public int Value { get; set; }

        public void Increment()
        {
            Value++;
        }

    }
}
