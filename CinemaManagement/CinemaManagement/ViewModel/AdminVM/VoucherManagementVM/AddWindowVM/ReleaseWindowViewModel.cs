﻿using CinemaManagement.DTOs;
using CinemaManagement.Models.Services;
using CinemaManagement.Utils;
using CinemaManagement.Views.Admin.VoucherManagement.AddWindow;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CinemaManagement.ViewModel.AdminVM.VoucherManagementVM
{
    public partial class VoucherViewModel : BaseViewModel
    {
        public static int NumberCustomer;


        private DateTime _ReleaseDate;
        public DateTime ReleaseDate
        {
            get { return _ReleaseDate; }
            set { _ReleaseDate = value; OnPropertyChanged(); }
        }

        private ComboBoxItem _ReleaseCustomerList;

        public ComboBoxItem ReleaseCustomerList
        {
            get { return _ReleaseCustomerList; }
            set { _ReleaseCustomerList = value; RefreshEmailList(); }
        }


        public ICommand DeleteWaitingReleaseCM { get; set; }
        public ICommand MoreEmailCM { get; set; }
        public ICommand LessEmailCM { get; set; }
        public ICommand OpenReleaseVoucherCM { get; set; }
        public ICommand ReleaseVoucherCM { get; set; }
        public ICommand ResetSelectedNumberCM { get; set; }

        private ObservableCollection<VoucherDTO> releaseVoucherList;
        public ObservableCollection<VoucherDTO> ReleaseVoucherList
        {
            get { return releaseVoucherList; }
            set { releaseVoucherList = value; OnPropertyChanged(); }
        }

        private ObservableCollection<CustomerEmail> _ListCustomerEmail;
        public ObservableCollection<CustomerEmail> ListCustomerEmail
        {
            get { return _ListCustomerEmail; }
            set { _ListCustomerEmail = value; OnPropertyChanged(); }
        }

        public async Task ReleaseVoucherFunc(ReleaseVoucher p)
        {
            string mess = "Số voucher không chia hết cho khách hàng!";
            if (WaitingMiniVoucher.Count == 0)
            {
                MessageBox.Show("Danh sách voucher đang trống!");
                return;
            }
            foreach (var item in ListCustomerEmail)
            {
                if (string.IsNullOrEmpty(item.Email))
                {
                    MessageBox.Show("Tồn tại email trống");
                    return;
                }
            }
            //top 5 customer
            if (NumberCustomer == 5)
            {
                if (WaitingMiniVoucher.Count > 5)
                {
                    if (WaitingMiniVoucher.Count % 5 != 0)
                    {
                        MessageBox.Show(mess);
                        return;
                    }
                }
                else if (WaitingMiniVoucher.Count < 5)
                {
                    MessageBox.Show(mess);
                    return;
                }

            }
            // input customer mail
            else if (NumberCustomer == -1)
            {
                if (ListCustomerEmail.Count == 0)
                {
                    MessageBox.Show("Danh sách khách hàng đang trống!");
                    return;
                }
                if (WaitingMiniVoucher.Count > ListCustomerEmail.Count)
                {
                    if (WaitingMiniVoucher.Count % ListCustomerEmail.Count != 0)
                    {
                        MessageBox.Show(mess);
                        return;
                    }
                }
                else if (WaitingMiniVoucher.Count < ListCustomerEmail.Count)
                {
                    MessageBox.Show(mess);
                    return;
                }
            }

            // Danh sách code và khách hàng
            List<string> listCode = releaseVoucherList.Select(v => v.Code).ToList();
            List<string> listCustomerEmail = ListCustomerEmail.Select(v => v.Email).ToList();

            //Chia danh sách code theo số lượng khách hàng
            int sizePerItem = listCode.Count / listCustomerEmail.Count;
            List<List<string>> ListCodePerEmailList = ChunkBy(listCode, sizePerItem);

            (bool sendSuccess, string messageFromSendEmail) = await sendHtmlEmail(listCustomerEmail, ListCodePerEmailList);

            if (!sendSuccess)
            {
                MessageBox.Show(messageFromSendEmail);
                return;
            }

            (bool releaseSuccess, string messageFromRelease) = await VoucherService.Ins.ReleaseMultiVoucher(WaitingMiniVoucher);

            if (releaseSuccess)
            {
                MessageBox.Show(messageFromRelease);
                WaitingMiniVoucher.Clear();
                (VoucherReleaseDTO voucherReleaseDetail, bool haveAnyUsedVoucher) = VoucherService.Ins.GetVoucherReleaseDetails(SelectedItem.Id);

                SelectedItem = voucherReleaseDetail;
                ListViewVoucher = new ObservableCollection<VoucherDTO>(SelectedItem.Vouchers);
                StoreAllMini = new List<VoucherDTO>(ListViewVoucher);
                AddVoucher.topcheck.IsChecked = false;
                AddVoucher.AllCheckBox.Clear();
                AddVoucher._cbb.SelectedIndex = 0;
                NumberSelected = 0;
                p.Close();
            }
            else
            {
                MessageBox.Show(messageFromRelease);
            }
        }
        public void RefreshEmailList()
        {
            if (ReleaseCustomerList is null) return;

            switch (ReleaseCustomerList.Content.ToString())
            {
                case "Top 5 khách hàng trong tháng":
                    {
                        (List<CustomerDTO> top5cus, _, _) = StatisticsService.Ins.GetTop5CustomerExpenseByMonth(DateTime.Today.Month);
                        ListCustomerEmail = new ObservableCollection<CustomerEmail>();

                        foreach (var item in top5cus)
                        {
                            ListCustomerEmail.Add(new CustomerEmail { Email = item.Email });
                        }

                        return;
                    }
                case "Khác":
                    {
                        ListCustomerEmail = new ObservableCollection<CustomerEmail>();
                        return;
                    }
                case "Khách hàng mới trong tháng":
                    {
                        ListCustomerEmail = new ObservableCollection<CustomerEmail>();
                        return;
                    }
            }
        }
        protected async Task<(bool, string)> sendHtmlEmail(List<string> customerEmailList, List<List<string>> ListCodePerEmailList)
        {
            List<Task> listSendEmailTask = new List<Task>();
            for (int i = 0; i < customerEmailList.Count; i++)
            {
                listSendEmailTask.Add(sendEmailForACustomer(customerEmailList[i], ListCodePerEmailList[i]));
            }

            try
            {
                await Task.WhenAll(listSendEmailTask);
                return (true, "Gửi thành công");
            }
            catch (Exception e)
            {
                return (false, e.Message);
            }
        }

        private Task sendEmailForACustomer(string customerEmail, List<string> listCode)
        {
            var appSettings = ConfigurationManager.AppSettings;
            string APP_EMAIL = appSettings["APP_EMAIL"];
            string APP_PASSWORD = appSettings["APP_PASSWORD"];

            //SMTP CONFIG
            SmtpClient smtp = new SmtpClient("smtp.gmail.com");
            smtp.EnableSsl = true;
            smtp.Port = 587;
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential(APP_EMAIL, APP_PASSWORD);

            MailMessage mail = new MailMessage();
            mail.IsBodyHtml = true;

            //create Alrternative HTML view

            AlternateView htmlView = AlternateView.CreateAlternateViewFromString(GetCustomerGratitudeTemplate(listCode), Encoding.UTF8, "text/html");
            //Add Image
            LinkedResource image = new LinkedResource(Helper.GetImagePath("poster.png"), "image/png");
            image.ContentId = "myImageID";
            image.ContentType.Name = "thank_you_picture";
            image.TransferEncoding = TransferEncoding.Base64;
            image.ContentLink = new Uri("cid:" + image.ContentId);

            //Add the Image to the Alternate view
            htmlView.LinkedResources.Add(image);
            //Add view to the Email Message
            mail.AlternateViews.Add(htmlView);

            mail.From = new MailAddress(APP_EMAIL, "Squadin Cinema");
            mail.To.Add(customerEmail);
            mail.Subject = "Tri ân khách hàng thân thiết";

            return smtp.SendMailAsync(mail);
        }

        private string GetCustomerGratitudeTemplate(List<string> listCode)
        {
            string templateHTML = Helper.GetEmailTemplatePath(GRATITUDE_TEMPLATE_FILE);
            string listVoucherHTML = "";

            for (int i = 0; i < listCode.Count; i++)
            {
                listVoucherHTML += VOUCHER_ITEM_HTML.Replace("{INDEX}", $"{i + 1}").Replace("{CODE_HERE}", listCode[i]);
            }


            String HTML = File.ReadAllText(templateHTML).Replace("{LIST_CODE_HERE}", listVoucherHTML);
            return HTML;
        }

        public List<List<string>> ChunkBy(List<string> source, int chunkSize)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }

        const string GRATITUDE_TEMPLATE_FILE = "top5_customer_gratitude_html.txt";
        const string VOUCHER_ITEM_HTML = "<li>Voucher {INDEX}: {CODE_HERE}</li>";
    }

    public class CustomerEmail
    {
        public string Email { get; set; }
        public CustomerEmail() { }
    }
}
