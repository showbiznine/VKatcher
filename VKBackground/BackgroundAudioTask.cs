using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK.API.Model;
using VKatcherShared;
using VKatcherShared.Messages;
using VKatcherShared.Models;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

namespace VKBackground
{
    public sealed class BackgroundAudioTask : IBackgroundTask
    {
        SystemMediaTransportControls smtc;
        private BackgroundTaskDeferral deferral;
        private AppState _foregroundAppState = AppState.Unknown;
        private ManualResetEvent _backgroundTaskStarted = new ManualResetEvent(false);
        private MediaPlaybackList _playbackList;
        private bool _autoRepeat, _shuffle;
        private bool _playbackStartedPreviously = false;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine("Background audio task " + taskInstance.Task.Name + " starting...");

            #region Set Up Transport Controls
            smtc = BackgroundMediaPlayer.Current.SystemMediaTransportControls;
            smtc.ButtonPressed += Smtc_ButtonPressed;
            smtc.PropertyChanged += Smtc_PropertyChanged;
            smtc.IsEnabled = true;
            smtc.IsPauseEnabled = true;
            smtc.IsPlayEnabled = true;
            smtc.IsNextEnabled = true;
            smtc.IsPreviousEnabled = true;
            smtc.IsRecordEnabled = true;
            #endregion

            #region To-do: App state
            var value = ApplicationSettingsHelper.ReadResetSettingsValue(ApplicationSettingsConstants.AppState);
            if (value == null)
                _foregroundAppState = AppState.Unknown;
            else
                _foregroundAppState = EnumHelper.Parse<AppState>(value.ToString());
            #endregion

            #region Set Up Media Player
            BackgroundMediaPlayer.Current.CurrentStateChanged += BMP_CurrentStateChanged;
            BackgroundMediaPlayer.MessageReceivedFromForeground += BackgroundMediaPlayer_MessageReceivedFromForeground;
            #endregion

            if (_foregroundAppState != AppState.Suspended)
                MessageService.SendMessageToForeground(new BackgroundAudioTaskStartedMessage());
            //Tell the foreground app that the background task has started

            ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.BackgroundTaskState, BackgroundTaskState.Running.ToString());
            //Save the application state as running

            deferral = taskInstance.GetDeferral();

            //Mark task as started
            _backgroundTaskStarted.Set();

            taskInstance.Task.Completed += Task_Completed;
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCancelled);
        }

        private void BMP_CurrentStateChanged(MediaPlayer sender, object args)
        {
            switch (sender.CurrentState)
            {
                case MediaPlayerState.Closed:
                    break;
                case MediaPlayerState.Opening:
                    break;
                case MediaPlayerState.Buffering:
                    break;
                case MediaPlayerState.Playing:
                    break;
                case MediaPlayerState.Paused:
                    break;
                case MediaPlayerState.Stopped:
                    break;
                default:
                    break;
            }
        }

        private void OnCancelled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            Debug.WriteLine("Background audio task " + sender.Task.TaskId + " cancel requested...");
            try
            {
                //Set as not running
                _backgroundTaskStarted.Reset();

                //To-do save state
                ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.TrackId, GetCurrentTrackURL() == null ? null : GetCurrentTrackURL().ToString());
                ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.Position, BackgroundMediaPlayer.Current.PlaybackSession.Position.ToString());
                ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.BackgroundTaskState, BackgroundTaskState.Canceled.ToString());
                ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.AppState, Enum.GetName(typeof(AppState), _foregroundAppState));

                //Unsubscribe from playbacklist changes
                if (_playbackList != null)
                {
                    _playbackList.CurrentItemChanged -= _playbackList_CurrentItemChanged;
                    _playbackList = null;
                }

                //Unsubscribe from event handlers
                BackgroundMediaPlayer.MessageReceivedFromForeground -= BackgroundMediaPlayer_MessageReceivedFromForeground;
                smtc.ButtonPressed -= Smtc_ButtonPressed;
                smtc.PropertyChanged -= Smtc_PropertyChanged;

                BackgroundMediaPlayer.Shutdown();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            deferral.Complete();
            Debug.WriteLine("Cancel complete...");
        }

        private void Task_Completed(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            Debug.WriteLine("BackgroundAudioTask " + sender.TaskId + " completed");
            deferral.Complete();
        }

        #region SMTC Functions and Handlers

        private void UpdateUVCOnNewTrack(MediaPlaybackItem item)
        {
            if (item == null)
            {
                smtc.PlaybackStatus = MediaPlaybackStatus.Stopped;
                smtc.DisplayUpdater.ClearAll();
                return;
            }

            smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
            smtc.DisplayUpdater.Type = MediaPlaybackType.Music;
            smtc.DisplayUpdater.MusicProperties.Title = item.Source.CustomProperties["title"].ToString();
            smtc.DisplayUpdater.MusicProperties.Artist = item.Source.CustomProperties["artist"].ToString();
            smtc.DisplayUpdater.Update();
        }

        private void Smtc_PropertyChanged(SystemMediaTransportControls sender, SystemMediaTransportControlsPropertyChangedEventArgs args)
        {
            if (args.Property == SystemMediaTransportControlsProperty.SoundLevel)
            {
                if (SystemMediaTransportControlsProperty.SoundLevel == 0)
                {
                    // If muted, pause the track
                }
                else
                {
                    //Resume the track
                }
            }
        }

        private void Smtc_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    StartPlayback();
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    BackgroundMediaPlayer.Current.Pause();
                    break;
                case SystemMediaTransportControlsButton.Stop:
                    break;
                case SystemMediaTransportControlsButton.Record:
                    break;
                case SystemMediaTransportControlsButton.Next:
                    SkipNext();
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    SkipPrevious();
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region Playlist Management

        //If this breaks, try getting rid of "private"
        private async void CreatePlaybackList(ObservableCollection<VKAudio> tracks)
        {
            if (_playbackList != null)
            {
                _playbackList.CurrentItemChanged -= _playbackList_CurrentItemChanged;
            }
            _playbackList = new MediaPlaybackList();
            _playbackList.AutoRepeatEnabled = _autoRepeat;
            _playbackList.ShuffleEnabled = _shuffle;

            foreach (var track in tracks)
            {
                var source = new object();
                bool dl = track.url.StartsWith("C:");
                if (dl)
                {
                    var sf = await StorageFile.GetFileFromPathAsync(track.url);
                    source = MediaSource.CreateFromStorageFile(sf);
                }
                else
                {
                    source = MediaSource.CreateFromUri(new Uri(track.url));
                }
                ((MediaSource)source).CustomProperties["track_url"] = track.url;
                ((MediaSource)source).CustomProperties["title"] = track.title;
                ((MediaSource)source).CustomProperties["artist"] = track.artist;
                ((MediaSource)source).CustomProperties["album_art"] = track.photo_url;
                _playbackList.Items.Add(new MediaPlaybackItem(((MediaSource)source)));
            }

            BackgroundMediaPlayer.Current.AutoPlay = false;
            BackgroundMediaPlayer.Current.Source = _playbackList;

            _playbackList.CurrentItemChanged += _playbackList_CurrentItemChanged;
        }

        private async void AddToPlaylist(VKAudio track)
        {
            var source = new object();
            bool dl = track.url.StartsWith("C:");
            if (dl)
            {
                var sf = await StorageFile.GetFileFromPathAsync(track.url);
                source = MediaSource.CreateFromStorageFile(sf);
            }
            else
            {
                source = MediaSource.CreateFromUri(new Uri(track.url));
            }
            ((MediaSource)source).CustomProperties["track_url"] = track.url;
            ((MediaSource)source).CustomProperties["title"] = track.title;
            ((MediaSource)source).CustomProperties["artist"] = track.artist;
            ((MediaSource)source).CustomProperties["album_art"] = track.photo_url;

            _playbackList.Items.Add(new MediaPlaybackItem((MediaSource)source));
            Debug.WriteLine("Added track to playlist");
        }

        private async void PlayNext(VKAudio track)
        {
            var source = new object();
            bool dl = track.url.StartsWith("C:");
            if (dl)
            {
                var sf = await StorageFile.GetFileFromPathAsync(track.url);
                source = MediaSource.CreateFromStorageFile(sf);
            }
            else
            {
                source = MediaSource.CreateFromUri(new Uri(track.url));
            }
            ((MediaSource)source).CustomProperties["track_url"] = track.url;
            ((MediaSource)source).CustomProperties["title"] = track.title;
            ((MediaSource)source).CustomProperties["artist"] = track.artist;
            ((MediaSource)source).CustomProperties["album_art"] = track.photo_url;

            var currentIndex = (int)_playbackList.CurrentItemIndex;
            _playbackList.Items.Insert(currentIndex + 1, new MediaPlaybackItem((MediaSource)source));
            Debug.WriteLine("Added item at index: " + currentIndex);
        }

        private void _playbackList_CurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
        {
            var item = args.NewItem;
            string str = item == null ? "null" : GetTrackURL(item);
            Debug.WriteLine("Changing playback list to item: " + str);

            UpdateUVCOnNewTrack(item); //Update UVC

            //Get the current track
            string currentTrackUrl = null;
            long currentTrackID = 0;
            if (item != null)
                currentTrackUrl = item.Source.CustomProperties["track_url"].ToString();


            //Notify foreground of change
            if (_foregroundAppState == AppState.Active && item != null)
                MessageService.SendMessageToForeground(new TrackChangedMessage(new Uri(currentTrackUrl)));

            else

                ApplicationSettingsHelper.SaveSettingsValue("trackid", currentTrackUrl == null ? null : currentTrackUrl.ToString());

        }
      
        private void StartPlayback()
        {
            try
            {
                // If playback was already started once we can just resume playing.
                if (!_playbackStartedPreviously)
                {
                    _playbackStartedPreviously = true;
                    
                    // If the task was cancelled we would have saved the current track and its position. We will try playback from there.
                    var currentTrackID = ApplicationSettingsHelper.ReadResetSettingsValue(ApplicationSettingsConstants.TrackId);
                    var currentTrackPosition = ApplicationSettingsHelper.ReadResetSettingsValue(ApplicationSettingsConstants.Position);

                    if (currentTrackID != null)
                    {
                        // Find the index of the item by name
                        var index = _playbackList.Items.ToList().FindIndex(item =>
                            GetTrackURL(item).ToString() == (string)currentTrackID);

                        if (currentTrackPosition == null)
                        {
                            // Play from start if we dont have position
                            Debug.WriteLine("StartPlayback: Switching to track " + index);
                            _playbackList.MoveTo((uint)index);

                            // Begin playing
                            BackgroundMediaPlayer.Current.Play();
                        }
                        else
                        {
                            // Play from exact position otherwise
                            TypedEventHandler<MediaPlaybackList, CurrentMediaPlaybackItemChangedEventArgs> handler = null;
                            handler = (MediaPlaybackList list, CurrentMediaPlaybackItemChangedEventArgs args) =>
                            {
                                if (args.NewItem == _playbackList.Items[index])
                                {
                                    // Unsubscribe because this only had to run once for this item
                                    _playbackList.CurrentItemChanged -= handler;

                                    // Set position
                                    var position = TimeSpan.Parse((string)currentTrackPosition);
                                    Debug.WriteLine("StartPlayback: Setting Position " + position);
                                    BackgroundMediaPlayer.Current.PlaybackSession.Position = position;

                                    // Begin playing
                                    BackgroundMediaPlayer.Current.Play();
                                }
                            };

                            _playbackList.CurrentItemChanged += handler;

                            // Switch to the track which will trigger an item changed event
                            Debug.WriteLine("StartPlayback: Switching to track " + index);
                            _playbackList.MoveTo((uint)index);
                        }
                    }
                    else
                    {
                        // Begin playing
                        BackgroundMediaPlayer.Current.Play();
                    }
                }
                else
                {
                    // Begin playing
                    BackgroundMediaPlayer.Current.Play();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private void SkipNext()
        {
            smtc.PlaybackStatus = MediaPlaybackStatus.Changing;
            _playbackList.MoveNext();
            BackgroundMediaPlayer.Current.Play();
        }

        private void SkipPrevious()
        {
            smtc.PlaybackStatus = MediaPlaybackStatus.Changing;
            if (BackgroundMediaPlayer.Current.PlaybackSession.Position.Seconds < 10)
            {
                //If early on, go to previous
                _playbackList.MovePrevious(); 
            }
            else
            {
                //Go back to start
                BackgroundMediaPlayer.Current.PlaybackSession.Position = new TimeSpan(0);
            }
            BackgroundMediaPlayer.Current.Play();
        }

        private void BackgroundMediaPlayer_MessageReceivedFromForeground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            AppSuspendedMessage appSuspendedMessage;
            if (MessageService.TryParseMessage(e.Data, out appSuspendedMessage))
            {
                Debug.WriteLine("App suspending"); // App is suspended, you can save your task state at this point
                _foregroundAppState = AppState.Suspended;
                var currentTrackId = GetCurrentTrackURL();
                ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.TrackId, currentTrackId == null ? null : currentTrackId.ToString());
                return;
            }

            AppResumedMessage appResumedMessage;
            if (MessageService.TryParseMessage(e.Data, out appResumedMessage))
            {
                Debug.WriteLine("App resuming"); // App is resumed, now subscribe to message channel
                _foregroundAppState = AppState.Active;
                return;
            }

            StartPlaybackMessage startPlaybackMessage;
            if (MessageService.TryParseMessage(e.Data, out startPlaybackMessage))
            {
                //Foreground App process has signalled that it is ready for playback
                Debug.WriteLine("Starting Playback");
                StartPlayback();
                return;
            }

            SkipNextMessage skipNextMessage;
            if (MessageService.TryParseMessage(e.Data, out skipNextMessage))
            {
                // User has chosen to skip track from app context.
                Debug.WriteLine("Skipping to next");
                SkipNext();
                return;
            }

            SkipPreviousMessage skipPreviousMessage;
            if (MessageService.TryParseMessage(e.Data, out skipPreviousMessage))
            {
                // User has chosen to skip track from app context.
                Debug.WriteLine("Skipping to previous");
                SkipPrevious();
                return;
            }

            TrackChangedMessage trackChangedMessage;
            if (MessageService.TryParseMessage(e.Data, out trackChangedMessage))
            {
                var list = _playbackList.Items.ToList();
                int index;
                if (!trackChangedMessage.TrackURL.ToString().StartsWith("file"))
                {
                    index = list.FindIndex(i => (string)i.Source.CustomProperties["track_url"] == trackChangedMessage.TrackURL.ToString());
                }
                else
                {
                    index = list.FindIndex(i => (string)i.Source.CustomProperties["track_url"] == trackChangedMessage.TrackURL.LocalPath.ToString());
                }
                Debug.WriteLine("Skipping to track " + index);
                smtc.PlaybackStatus = MediaPlaybackStatus.Changing;
                try
                {
                    _playbackList.MoveTo((uint)index);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

                BackgroundMediaPlayer.Current.Play();
                return;
            }

            UpdatePlaylistMessage updatePlaylistMessage;
            if (MessageService.TryParseMessage(e.Data, out updatePlaylistMessage))
            {
                CreatePlaybackList(updatePlaylistMessage.Songs);
                    return;
            }

            AddToPlaylistMessage addToPlaylistMessage;
            if (MessageService.TryParseMessage(e.Data, out addToPlaylistMessage))
            {
                PlayNext(addToPlaylistMessage.Track);
                return;
            }

            PlayNextMessage playNextMessage;
            if (MessageService.TryParseMessage(e.Data, out playNextMessage))
            {
                PlayNext(playNextMessage.Track);
                return;
            }
        }
        #endregion

        #region Helpers
        private string GetTrackURL(MediaPlaybackItem item)
        {
            if (item == null)
                return null;

            return item.Source.CustomProperties["track_url"].ToString();
        }

        string GetCurrentTrackURL()
        {
            if (_playbackList == null)
                return null;
            return GetTrackURL(_playbackList.CurrentItem);
        }
        #endregion
    }
}
