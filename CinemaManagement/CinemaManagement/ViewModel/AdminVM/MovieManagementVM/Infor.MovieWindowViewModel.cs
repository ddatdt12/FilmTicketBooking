﻿using CinemaManagement.DTOs;
using CinemaManagement.Utils;
using CinemaManagement.Views.Admin.QuanLyPhimPage;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;

namespace CinemaManagement.ViewModel.AdminVM.MovieManagementVM
{
    public partial class MovieManagementViewModel : BaseViewModel
    {
        public ICommand LoadInforMovieCM { get; set; }



        public void LoadInforMovie(InforMovieWindow w1)
        {
            List<GenreDTO> tempgenre = new List<GenreDTO>(SelectedItem.Genres);


            movieName = SelectedItem.DisplayName;
            w1.Genre.Text = tempgenre[0].DisplayName;
            movieDirector = SelectedItem.Director;
            movieCountry = SelectedItem.Country;
            movieDuration = SelectedItem.RunningTime.ToString();
            movieDes = SelectedItem.Description;
            movieYear = SelectedItem.ReleaseYear.ToString();
            w1.Year.Text = SelectedItem.ReleaseYear.ToString();

            if (File.Exists(Helper.GetMovieImgPath(SelectedItem.Image)))
            {
                ImageSource = Helper.GetImageSource(SelectedItem.Image);
            }
            else
            {
                w1.imageframe.Source = Helper.GetImageSource("null.jpg");
            }
        }
    }
}
