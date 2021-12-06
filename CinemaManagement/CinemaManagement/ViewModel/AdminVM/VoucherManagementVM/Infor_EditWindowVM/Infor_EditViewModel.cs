﻿using CinemaManagement.DTOs;
using CinemaManagement.Models.Services;
using CinemaManagement.Views;
using CinemaManagement.Views.Admin.VoucherManagement.AddWindow;
using CinemaManagement.Views.Admin.VoucherManagement.Infor_EditWindow;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CinemaManagement.ViewModel.AdminVM.VoucherManagementVM
{
    public partial class VoucherViewModel : BaseViewModel
    {

        #region binding data
        private string _Name;
        public string Name
        {
            get { return _Name; }
            set { _Name = value; OnPropertyChanged(); }
        }

        private int par;
        public int Par
        {
            get { return par; }
            set { par = value; OnPropertyChanged(); }
        }

        private static bool status2;
        public static bool Status2
        {
            get { return status2; }
            set { status2 = value; }
        }

        private DateTime start;
        public DateTime Start
        {
            get { return start; }
            set { start = value; OnPropertyChanged(); }
        }

        private DateTime finish;
        public DateTime Finish
        {
            get { return finish; }
            set { finish = value; OnPropertyChanged(); }
        }

        private bool merge;
        public bool Merge
        {
            get { return merge; }
            set { merge = value; OnPropertyChanged(); }
        }

        private ComboBoxItem _Object;
        public ComboBoxItem Object
        {
            get { return _Object; }
            set { _Object = value; OnPropertyChanged(); }
        }

        private decimal minimum;
        public decimal Minimum
        {
            get { return minimum; }
            set { minimum = value; OnPropertyChanged(); }
        }
        #endregion

        VoucherReleaseDTO oldVer = new VoucherReleaseDTO();


        private static List<int> waitingMiniVoucher;
        public static List<int> WaitingMiniVoucher
        {
            get { return waitingMiniVoucher; }
            set { waitingMiniVoucher = value; }
        }

        private int numberSelected;
        public int NumberSelected
        {
            get { return numberSelected; }
            set { numberSelected = value; OnPropertyChanged(); }
        }


        public static bool HaveUsed = false;

        public ICommand LoadInforBigVoucherCM { get; set; }
        public ICommand LoadInforCM { get; set; }
        public ICommand UpdateBigVoucherCM { get; set; }
        public ICommand DeleteMiniVoucherCM { get; set; }
        public ICommand StoreWaitingListCM { get; set; }
        public ICommand CheckAllMiniVoucherCM { get; set; }

        public void LoadEdit_InforViewDataFunc(Edit_InforPage w)
        {
            Name = SelectedItem.ReleaseName;
            Par = SelectedItem.ParValue;
            Status2 = SelectedItem.Status;
            if (Status2)
            {
                w.yes.IsChecked = true;
                w.no.IsChecked = false;
            }
            else
            {
                w.yes.IsChecked = false;
                w.no.IsChecked = true;
            }
            Start = SelectedItem.StartDate;
            Finish = SelectedItem.FinishDate;
            Merge = SelectedItem.EnableMerge;
            Object.Content = SelectedItem.ObjectType;
            Minimum = SelectedItem.MinimumOrderValue;
            w.unused.Content = SelectedItem.UnusedVCount;
            w.total.Content = SelectedItem.VCount;
        }
        public async Task UpdateBigVoucherFunc()
        {
            if (string.IsNullOrEmpty(Name))
            {
                MessageBoxCustom mb = new MessageBoxCustom("", "Vui lòng nhập đủ thông tin", MessageType.Warning, MessageButtons.OK);
                mb.ShowDialog();
                return;
            }
            if (SelectedItem != null)
                oldVer = SelectedItem;
            VoucherReleaseDTO vr = new VoucherReleaseDTO
            {
                Id = SelectedItem.Id,
                StartDate = Start,
                FinishDate = Finish,
                EnableMerge = Merge,
                MinimumOrderValue = Minimum,
                ObjectType = Object.Content.ToString(),
                ParValue = Par,
                ReleaseName = Name,
                StaffId = BindStaffID,
                Status = Status2,
            };

            (bool isSucess, string addSuccess) = await VoucherService.Ins.UpdateVoucherRelease(vr);

            if (isSucess)
            {
                MessageBoxCustom mb = new MessageBoxCustom("", addSuccess, MessageType.Success, MessageButtons.OK);
                mb.ShowDialog();

                ListBigVoucher = new ObservableCollection<VoucherReleaseDTO>(await VoucherService.Ins.GetAllVoucherReleases());
                (VoucherReleaseDTO voucherReleaseDetail, _) = await VoucherService.Ins.GetVoucherReleaseDetails(oldVer.Id);
                SelectedItem = voucherReleaseDetail;
                ListViewVoucher = new ObservableCollection<VoucherDTO>(SelectedItem.Vouchers);
                StoreAllMini = new ObservableCollection<VoucherDTO>(ListViewVoucher);
                NumberSelected = 0;
            }
            else
            {
                MessageBoxCustom mb = new MessageBoxCustom("", addSuccess, MessageType.Error, MessageButtons.OK);
                mb.ShowDialog();
            }
        }
        public async Task DeleteMiniVoucherFunc()
        {
            if (WaitingMiniVoucher.Count == 0)
            {
                MessageBoxCustom mb = new MessageBoxCustom("", "Danh sách chọn đang trống!", MessageType.Warning, MessageButtons.OK);
                mb.ShowDialog();
                return;
            }

            (bool deleteSuccess, string messageFromDelete) = await VoucherService.Ins.DeteleVouchers(WaitingMiniVoucher);

            if (deleteSuccess)
            {
                AddVoucher.AllCheckBox.Clear();
                MessageBoxCustom mb = new MessageBoxCustom("", messageFromDelete, MessageType.Success, MessageButtons.OK);
                mb.ShowDialog();
                (VoucherReleaseDTO voucherReleaseDetail, bool haveAnyUsedVoucher) = await VoucherService.Ins.GetVoucherReleaseDetails(SelectedItem.Id);

                SelectedItem = voucherReleaseDetail;
                ListViewVoucher = new ObservableCollection<VoucherDTO>(SelectedItem.Vouchers);
                StoreAllMini = new ObservableCollection<VoucherDTO>(ListViewVoucher);
                AddVoucher.topcheck.IsChecked = false;
                NumberSelected = 0;
            }
            else
            {
                MessageBoxCustom mb = new MessageBoxCustom("", messageFromDelete, MessageType.Error, MessageButtons.OK);
                mb.ShowDialog();
            }
        }
        public void CheckAllMiniVoucherFunc(bool func)
        {
            if (func)
            {
                WaitingMiniVoucher.Clear();
                foreach (var item in ListViewVoucher)
                {
                    if (item.Status != "Ðã phát hành")
                        WaitingMiniVoucher.Add(item.Id);
                }
                NumberSelected = WaitingMiniVoucher.Count;
            }
            else
            {
                WaitingMiniVoucher.Clear();
                NumberSelected = 0;
            }
        }
    }
}
