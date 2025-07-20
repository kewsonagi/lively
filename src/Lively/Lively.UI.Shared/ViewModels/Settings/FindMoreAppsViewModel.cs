using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Collections;
using Lively.Common.Factories;
using Lively.Common.Helpers;
using Lively.Common.Services;
using Lively.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Lively.UI.Shared.ViewModels
{
    public partial class FindMoreAppsViewModel : ObservableObject
    {
        private readonly IApplicationsFactory appFactory;
        private readonly IFileService fileService;

        [ObservableProperty]
        private ObservableCollection<ApplicationModel> applications = [];

        [ObservableProperty]
        private AdvancedCollectionView applicationsFiltered;

        [ObservableProperty]
        private ApplicationModel selectedItem;

        public FindMoreAppsViewModel(IApplicationsFactory appFactory, IFileService fileService)
        {
            this.appFactory = appFactory;
            this.fileService = fileService;

            ApplicationsFiltered = new AdvancedCollectionView(Applications, true);
            ApplicationsFiltered.SortDescriptions.Add(new SortDescription("AppName", SortDirection.Ascending));

            using (ApplicationsFiltered.DeferRefresh())
            {
                foreach (var process in Process.GetProcesses())
                {
                    var hwnd = process.MainWindowHandle;
                    if (hwnd == IntPtr.Zero || WindowUtil.IsUWPApp(hwnd) || !WindowUtil.IsVisibleTopLevelWindows(hwnd))
                        continue;

                    var app = appFactory.CreateApp(hwnd);
                    if (app is not null)
                        Applications.Add(app);
                }
            }
            SelectedItem = Applications.FirstOrDefault();
        }

        private RelayCommand _browseCommand;
        public RelayCommand BrowseCommand => _browseCommand ??= new RelayCommand(async() => await BrowseApp());

        private async Task BrowseApp()
        {
            var files = await fileService.PickFileAsync([".exe"]);
            if (files.Any())
            {
                var app = appFactory.CreateApp(files[0]);
                if (app is not null)
                {
                    Applications.Add(app);
                    SelectedItem = app;
                }
            }
        }
    }
}
