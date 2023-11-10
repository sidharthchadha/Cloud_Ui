﻿/******************************************************************************
 * Filename    = SessionsPage.xaml.cs
 *
 * Author      = Sidharth Chadha
 * 
 * Project     = Cloud_UX
 *
 * Description = Defines the View Model of the Sessions Page.
 *****************************************************************************/

using ServerlessFunc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;

namespace Cloud_UX
{
    public class SessionsViewModel :
        INotifyPropertyChanged // Notifies clients that a property value has changed.
    {
        /// <summary>
        /// Creates an instance of the Sessions ViewModel.
        /// Gets the details of the sessions conducted by the user.
        /// Then dispatch the changes to the view.
        /// <param name="userName">The username of the user.</param>
        /// </summary>
        public SessionsViewModel(string userName)
        {
            _model = new();
            GetSessions(userName);
            Trace.WriteLine("[Cloud] Sessions View Model created");
        }

        /// <summary>
        /// Gets the details of the sessions conducted by the user.
        /// Then dispatch the changes to the view.
        /// <param name="userName">The username of the user.</param>
        /// </summary>
        public async void GetSessions(string userName)
        {
            IReadOnlyList<SessionEntity> sessionsList = await _model.GetSessionsDetails(userName);
            Trace.WriteLine("[Cloud] Session details received");
            _ = this.ApplicationMainThreadDispatcher.BeginInvoke(
                        DispatcherPriority.Normal,
                        new Action<IReadOnlyList<SessionEntity>>((sessionsList) =>
                        {
                            lock (this)
                            {
                                this.ReceivedSessions = sessionsList;

                                this.OnPropertyChanged("ReceivedSessions");
                            }
                        }),
                        sessionsList);
        }

        /// <summary>
        /// List to store the sessions conducted.
        /// </summary>
        public IReadOnlyList<SessionEntity>? ReceivedSessions { get; set; }

        /// <summary>
        /// Property changed event raised when a property is changed on a component.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Handles the property changed event raised on a component.
        /// </summary>
        /// <param name="property">The name of the property.</param>
        public void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        /// <summary>
        /// Gets the dispatcher to the main thread. In case it is not available
        /// (such as during unit testing) the dispatcher associated with the
        /// current thread is returned.
        /// </summary>
        private Dispatcher ApplicationMainThreadDispatcher =>
            (System.Windows.Application.Current?.Dispatcher != null) ?
                    System.Windows.Application.Current.Dispatcher :
                    Dispatcher.CurrentDispatcher;

        /// <summary>
        /// Underlying data model.
        /// </summary>
        private SessionsModel _model;
    }

}
