using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace PhotoViewer
{
    public partial class PhotoDisplay : Form
    {
        /// <summary>
        /// Blank bitmap
        /// </summary>
        private Bitmap bmpBlack;
        
        /// <summary>
        /// Current bitmap image to display.
        /// </summary>
        private Bitmap bmpDisplayed;

        /// <summary>
        /// Current photo file name
        /// </summary>
        public string FileName;

        /// <summary>
        /// Constructor
        /// </summary>
        public PhotoDisplay()
        {
            InitializeComponent();
            try
            {

            }
            catch(Exception e)
            {

            }
        }

        /// <summary>
        /// Resizes the visual elements to fit the display screen
        /// </summary>
        public void ResizePane()
        {
            try
            {
                this.picFeaturedPicture.Padding = new Padding(0);
                this.picFeaturedPicture.SetBounds(0, 5,this.Width,this.Height);
                this.axWindowsMediaPlayer.SetBounds(0, 5, this.Width, this.Height);
            }
            catch(Exception e)
            {

            }
        }

        /// <summary>
        /// Plays the video with the given file name.
        /// </summary>
        /// <param name="fileName"></param>
        public void PlayVideo(string fileName)
        {
            //dispose of current video
            try
            {
                this.axWindowsMediaPlayer.Ctlcontrols.stop();
                this.axWindowsMediaPlayer.currentPlaylist.clear();
            }
            catch(Exception ex)
            {

            }

            try
            {
                this.axWindowsMediaPlayer.settings.autoStart = true;
                this.axWindowsMediaPlayer.settings.enableErrorDialogs = false;
                this.axWindowsMediaPlayer.settings.mute = true;
                this.axWindowsMediaPlayer.settings.volume = 0;
                this.axWindowsMediaPlayer.URL = fileName;
                this.axWindowsMediaPlayer.Ctlcontrols.next();
                this.axWindowsMediaPlayer.Ctlcontrols.play();
                this.axWindowsMediaPlayer.stretchToFit = true;

                this.axWindowsMediaPlayer.uiMode = "none";
                this.axWindowsMediaPlayer.SetBounds(0, 0, this.Width, this.Height);
                this.axWindowsMediaPlayer.Show();
            }
            catch(Exception e)
            {

            }
        }

        /// <summary>
        /// Gets the player object
        /// </summary>
        /// <returns></returns>
        public AxWMPLib.AxWindowsMediaPlayer getPlayer()
        {
            return this.axWindowsMediaPlayer;
        }

        /// <summary>
        /// Stops playing the current video and clears the playlist.
        /// </summary>
        public void stopVideo()
        {

            try
            {
                this.axWindowsMediaPlayer.Hide();
                this.axWindowsMediaPlayer.Ctlcontrols.stop();
                this.axWindowsMediaPlayer.currentPlaylist.clear();
            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// Sets the current photo using the given file name
        /// </summary>
        /// <param name="fileName"></param>
        public void SetPhoto(string fileName)
        {
            this.FileName = fileName;
            this.SetPhoto(new Bitmap(fileName));
        }

        /// <summary>
        /// Sets the current photo from the pitmap
        /// </summary>
        /// <param name="bmp"></param>
        public void SetPhoto(Bitmap bmp)
        {
            try
            {
                this.stopVideo();
                if (this.picFeaturedPicture.Image != null)
                    this.picFeaturedPicture.Image.Dispose();
                this.bmpDisplayed = bmp;
                this.picFeaturedPicture.Image = (Image)this.bmpDisplayed;
                this.picFeaturedPicture.Show();
            }
            catch(Exception e)
            {

            }
        }

        /// <summary>
        /// Toggles the visibility of the photo picturebox
        /// </summary>
        /// <returns></returns>
        public bool TogglePhoto()
        {
            this.picFeaturedPicture.Visible = !this.picFeaturedPicture.Visible;
            return this.picFeaturedPicture.Visible;
        }

        /// <summary>
        /// Hides the photo picturebox
        /// </summary>
        public void HidePhoto()
        {
            this.picFeaturedPicture.Visible = false;
        }

        /// <summary>
        /// Shows the photo picturebox.
        /// </summary>
        public void ShowPhoto()
        {
            this.picFeaturedPicture.Visible = true;
        }

        /// <summary>
        /// Checks if the picture is viewable
        /// </summary>
        /// <returns></returns>
        public bool IsViewable()
        {
            return this.picFeaturedPicture.Visible;
        }

        /// <summary>
        /// Removes the picture
        /// </summary>
        public void RemovePhoto()
        {
            if (this.picFeaturedPicture.Image != null)
                this.picFeaturedPicture.Image.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void picFeaturedPicture_DoubleClick(object sender, EventArgs e)
        {
            
        }

        /// <summary>
        /// Processes keyboard commands.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PhotoDisplay_KeyPress(object sender, KeyPressEventArgs e)
        {
            switch(e.KeyChar.ToString().ToLower())
            {
                case "q":
                    //this.Hide();
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PhotoDisplay_DoubleClick(object sender, EventArgs e)
        {
            //this.picFeaturedPicture.Visible = !this.picFeaturedPicture.Visible;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PhotoDisplay_MouseEnter(object sender, EventArgs e)
        {
            //Cursor.Hide();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PhotoDisplay_MouseLeave(object sender, EventArgs e)
        {
            //Cursor.Show();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public Cursor GetCursor(byte[] array)
        {
            using (MemoryStream memoryStream = new MemoryStream(array))
            {
                return new Cursor(memoryStream);
            }
        }
    }
}
