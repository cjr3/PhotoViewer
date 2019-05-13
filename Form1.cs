using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

/// <summary>
/// Namespace for the photo viewer
/// </summary>
namespace PhotoViewer
{
    public partial class frmMain : Form
    {
        /// <summary>
        /// Paintbrush for creating the empty bitmap
        /// </summary>
        private SolidBrush painter;

        /// <summary>
        /// Flag to enable testing features such as writing to console or not.
        /// </summary>
        private bool testing = false;

        /// <summary>
        /// Current picture on screen.
        /// </summary>
        private string strCurrentPicture;

        /// <summary>
        /// Next picture in the cycle
        /// </summary>
        private string strNextPicture;

        /// <summary>
        /// Preview picture path
        /// </summary>
        private string strPreviewPicture;

        /// <summary>
        /// Folder to watch.
        /// </summary>
        private string strCurrentFolder;

        /// <summary>
        /// Recent photo added.
        /// </summary>
        private string strRecentPhoto;

        /// <summary>
        /// Bitmap for hiding other bitmaps in picture boxes
        /// </summary>
        private Bitmap bmpBlank;

        /// <summary>
        /// Bitmap for the current photo
        /// </summary>
        private Bitmap bmpCurrent;

        /// <summary>
        /// Bitmap for preview when selecting from the list.
        /// </summary>
        private Bitmap bmpPreview;

        /// <summary>
        /// Bitmap for next photo in gallery queue.
        /// </summary>
        private Bitmap bmpNext;

        /// <summary>
        /// Bitmap for recently added photo
        /// </summary>
        private Bitmap bmpRecentPhoto;

        /// <summary>
        /// Timer for recent photo display
        /// </summary>
        private Timer tmRecentPhoto;

        /// <summary>
        /// Timer for gallery
        /// </summary>
        private Timer tmCurrentPhoto;

        /// <summary>
        /// Flag that determines if the gallery
        /// </summary>
        private bool playing = false;

        /// <summary>
        /// Current photo index
        /// </summary>
        private int photoIndex = 0;

        /// <summary>
        /// List of sponsor image files
        /// </summary>
        private List<string> SponsorPhotos;

        /// <summary>
        /// True when showing sponsor, false when not.
        /// </summary>
        private bool sponsorFlag;

        /// <summary>
        /// Sponsor index is always incremental, not random.
        /// </summary>
        private int sponsorIndex = 0;

        /// <summary>
        /// Displays the photo at full screen.
        /// </summary>
        private PhotoDisplay GalleryDisplay;

        /// <summary>
        /// Preview photo dialog
        /// </summary>
        private PhotoDisplay PhotoPreview;

        private bool videoPlaying = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public frmMain()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Triggered when the form is loaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void frmMain_Load(object sender, EventArgs e)
        {
            this.LoadMonitors();

            //initialize sponsor photos
            this.SponsorPhotos = new List<string>();

            //initialize gallery window
            this.initGallery();
            this.CreatePaintbrush();
            tmRecentPhoto = new Timer();
            tmRecentPhoto.Tick += this.WipeRecentPhoto;
            tmRecentPhoto.Interval = 1000 * 15;

            tmCurrentPhoto = new Timer();
            tmCurrentPhoto.Tick += this.ProcessGallery;
            tmCurrentPhoto.Interval = 1000 * ((int) nmPhotoInterval.Value);

            //setup watcher
            wtrFolder.Changed += this.FileChanged;
            wtrFolder.Deleted += this.FileDeleted;
            wtrFolder.Renamed += this.FileRenamed;
            wtrFolder.Created += this.FileCreated;
            wtrFolder.EnableRaisingEvents = this.chkDynamic.Checked;

            //test();

            //Load the most recent folder in the user's MyPictures folder.
            if (!testing)
            {
                try
                {
                    string pics = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    string[] folders = Directory.GetDirectories(pics);

                    DateTime lastHigh = new DateTime(1900, 1, 1);
                    string lastDirectory = "";
                    if(folders.Length > 0)
                    {
                        foreach(string folder in folders)
                        {
                            DirectoryInfo di = new DirectoryInfo(folder);
                            DateTime dc = di.CreationTime;
                            if(dc > lastHigh)
                            {
                                lastDirectory = folder;
                                lastHigh = dc;
                            }
                        }
                    }

                    if(lastDirectory.Equals(""))
                    {
                        this.SetFolder(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
                    }
                    else
                    {
                        this.SetFolder(lastDirectory);
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Test code for development.
        /// </summary>
        private void test()
        {
            this.testing = true;
        }

        /// <summary>
        /// Logs the given message to the console.
        /// Works only when testing.
        /// </summary>
        /// <param name="text"></param>
        private void log(string text)
        {
            if (!this.testing)
                return;
            Console.WriteLine(text);
        }

        /// <summary>
        /// Reloads everything, clearing the list and photos.
        /// </summary>
        private void reload()
        {
            //pause watcher
            wtrFolder.EnableRaisingEvents = false;

            //empty list of pictures
            this.lstPictures.Items.Clear();

            //release image thumbnails
            try
            {
                this.ClearCurrentPhoto();
                this.ClearNextPhoto();
                this.ClearPreviewPhoto();
                if(this.GalleryDisplay != null)
                {
                    this.GalleryDisplay.RemovePhoto();
                }
            }
            catch(Exception e)
            {
                this.log("reload: " + e.Message);
            }
        }

        /// <summary>
        /// Creates the painting objects for clearing photos.
        /// </summary>
        private void CreatePaintbrush()
        {
            //create brush
            painter = new SolidBrush(Color.Black);

            //create bitmaps
            bmpBlank = new Bitmap(picCurrent.Width, picCurrent.Height);

            //paint blanks on each bitmap
            Graphics g = Graphics.FromImage(bmpBlank);
            g.FillRectangle(
                painter,
                new Rectangle(0,0, picCurrent.Width, picCurrent.Height)
            );
            g.Dispose();
        }

        /// <summary>
        /// Adds the given photo to the list.
        /// </summary>
        /// <param name="fileName"></param>
        private void AddPhoto(string fileName)
        {
            ListViewItem item = new ListViewItem(Path.GetFileName(fileName));
            item.SubItems.Add(fileName);
            lstPictures.Items.Add(item);
        }

        /// <summary>
        /// Adds the photos found in the given folder.
        /// </summary>
        /// <param name="folderName"></param>
        private void AddPhotos(string folderName)
        {
            try
            {
                string[] files = Directory.GetFiles(folderName);
                if(files.Length > 0)
                {
                    foreach(string fileName in files)
                    {
                        if(this.isPhoto(fileName))
                            this.AddPhoto(fileName);

                        //if (File.Exists(fileName))
                        //{
                        //    FileInfo file = new FileInfo(fileName);
                        //    string ext = file.Extension.ToLower();
                        //    switch (ext)
                        //    {
                        //        case ".jpg":
                        //        case ".jpeg":
                        //        case ".png":
                        //        case ".gif":
                        //            this.AddPhoto(fileName);
                        //            break;
                        //    }
                        //}
                    }
                }
            }
            catch(Exception e)
            {

            }
        }

        /// <summary>
        /// Sets the current photo image.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool SetCurrentPhoto(string name)
        {
            this.log("SetCurrentPhoto: " + name);
            try
            {
                this.ClearCurrentPhoto();
                bmpCurrent = new Bitmap(name);
                this.picCurrent.Image = (Image)bmpCurrent;
                this.strCurrentPicture = name;

                if(this.GalleryDisplay != null && !this.GalleryDisplay.IsDisposed)
                {
                    this.GalleryDisplay.SetPhoto(name);
                }

                return true;
            }
            catch(Exception e)
            {
                this.log("SetCurrentPhoto: " + e.Message);
            }

            return false;
        }

        /// <summary>
        /// Clears the current photo image.
        /// </summary>
        private void ClearCurrentPhoto()
        {
            if (this.picCurrent.Image != null && this.picCurrent.Image != bmpBlank)
                this.picCurrent.Image.Dispose();
            this.picCurrent.Image = (Image) bmpBlank;
            if (bmpCurrent != null && bmpCurrent != bmpBlank)
                bmpCurrent.Dispose();
            
            if (this.GalleryDisplay != null && !this.GalleryDisplay.IsDisposed)
            {
                this.GalleryDisplay.HidePhoto();
            }
        }

        /// <summary>
        /// Sets the preview photo image.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool SetPreviewPhoto(string name)
        {
            this.log("SetPreviewPhoto: " + name);
            try
            {
                this.ClearPreviewPhoto();
                bmpPreview = new Bitmap(name);
                this.picPreview.Image = (Image) bmpPreview;
                this.strPreviewPicture = name;
                return true;
            }
            catch(Exception e)
            {
                this.log("SetPreviewPhoto: " + e.Message);
            }

            return false;
        }

        /// <summary>
        /// Clears the preview photo image.
        /// </summary>
        private void ClearPreviewPhoto()
        {
            if (this.picPreview.Image != null && this.picPreview.Image != bmpBlank)
                this.picPreview.Image.Dispose();
            this.picPreview.Image = (Image)bmpBlank;
            if (bmpPreview != null && bmpPreview != bmpBlank)
                bmpPreview.Dispose();
        }

        /// <summary>
        /// Displays the most recent photo added dynamically.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool SetRecentPhoto(string name)
        {
            this.log("SetRecentPhoto: " + name);
            try
            {
                this.ClearRecentPhoto();
                bmpRecentPhoto = new Bitmap(name);
                this.picRecentPhoto.Image = (Image)bmpRecentPhoto;
                this.strRecentPhoto = name;

                if (tmRecentPhoto.Enabled)
                    tmRecentPhoto.Stop();
                tmRecentPhoto.Start();
                return true;
            }
            catch (Exception e)
            {
                this.log("SetRecentPhoto: " + e.Message);
            }

            return false;
        }

        /// <summary>
        /// Clears the recent photo image.
        /// </summary>
        private void ClearRecentPhoto()
        {
            if (this.picRecentPhoto.Image != null && this.picRecentPhoto.Image != bmpBlank)
                this.picRecentPhoto.Image.Dispose();
            this.picRecentPhoto.Image = (Image)bmpBlank;
            this.strRecentPhoto = null;
            if (bmpRecentPhoto != null && bmpRecentPhoto != bmpBlank)
                bmpRecentPhoto.Dispose();
        }

        /// <summary>
        /// Trigger called at the interval to remove the recent photo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WipeRecentPhoto(object sender, EventArgs e)
        {
            this.ClearRecentPhoto();
        }

        /// <summary>
        /// Sets the next photo, which will be sent to the screen
        /// after the interval passes.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool SetNextPhoto(string name)
        {
            this.log("SetNextPhoto: " + name);
            try
            {
                if (this.picCurrent.Image == null || this.picCurrent.Image == bmpBlank)
                {
                    return this.SetCurrentPhoto(name);
                }
                else
                {
                    this.ClearNextPhoto();
                    bmpNext = new Bitmap(name);
                    this.picNext.Image = (Image) bmpNext;
                    this.strNextPicture = name;
                }
                return true;
            }
            catch(Exception e)
            {
                this.log("SetNextPhoto: " + e.Message);
            }

            return false;
        }

        /// <summary>
        /// Clears the next photo image.
        /// </summary>
        private void ClearNextPhoto()
        {
            if (this.picNext.Image != null && this.picNext.Image != bmpBlank)
                this.picNext.Image.Dispose();
            this.picNext.Image = (Image)bmpBlank;
            if (bmpNext != null && bmpNext != bmpBlank)
                bmpNext.Dispose();
        }

        /// <summary>
        /// Sets the current folder and refreshes the list with the
        /// files in the folder.
        /// </summary>
        /// <param name="folderName"></param>
        public void SetFolder(string folderName)
        {
            this.reload();
            //this.AddAlbum(folderName);
            this.strCurrentFolder = folderName;
            this.lblCurrentFolder.Text = "Photos: " + folderName;
            this.AddPhotos(folderName);

            diagFolderSelector.SelectedPath = strCurrentFolder;

            //update watcher
            try
            {
                wtrFolder.Path = this.strCurrentFolder;
                wtrFolder.EnableRaisingEvents = true;
            }
            catch(Exception e)
            {
                this.log("SetFolder Error: " + e.Message);
            }
        }

        /// <summary>
        /// Sets the folder to pull a sponsor photo from.
        /// </summary>
        /// <param name="folderName"></param>
        public void SetSponsorFolder(string folderName)
        {
            this.lblSponsorFolder.Text = "Sponsors: " + folderName;
            this.SponsorPhotos.Clear();

            try
            {
                if (!Directory.Exists(folderName))
                    return;

                string[] files = Directory.GetFiles(folderName);
                if (files.Length <= 0)
                    return;

                foreach(string filename in files)
                {
                    if(this.isPhoto(filename))
                    {
                        this.SponsorPhotos.Add(filename);
                    }
                }

                if(this.SponsorPhotos.Count >= 1)
                {
                    this.sponsorIndex = -1;
                    this.sponsorFlag = false;
                    this.chkSponsors.Checked = true;
                }
                else
                {
                    this.chkSponsors.Checked = false;
                }
            }
            catch(Exception e)
            {

            }
        }

        /// <summary>
        /// Checks if the given file is valid for a photo
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        private bool isPhoto(string fname)
        {
            bool isFile = false;
            try
            {
                if(File.Exists(fname))
                {
                    FileInfo f = new FileInfo(fname);
                    switch (f.Extension.ToLower())
                    {
                        case ".jpg":
                        case ".gif":
                        case ".png":
                        case ".jpeg":
                            isFile = true;
                            break;
                    }
                }
            }
            catch(Exception e)
            {
                
            }
            return isFile;
        }

        /// <summary>
        /// Removes the given photo from the list.
        /// </summary>
        /// <param name="name"></param>
        private void RemovePhoto(string name)
        {

            if (name.Equals(this.strPreviewPicture))
                this.ClearPreviewPhoto();

            if (name.Equals(this.strNextPicture))
                this.ClearNextPhoto();

            if (name.Equals(this.strPreviewPicture))
                this.ClearPreviewPhoto();

            foreach(ListViewItem item in lstPictures.Items)
            {
                if (item.SubItems[1].Text.Equals(name))
                {
                    item.Remove();
                    break;
                }
            }
        }

        /// <summary>
        /// Removes the deleted file from the list.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="fe"></param>
        private void FileDeleted(object source, FileSystemEventArgs fe)
        {
            this.log("File Deleted: " + fe.FullPath);
            this.RemovePhoto(fe.FullPath);
        }

        /// <summary>
        /// Takes a renamed folder by removing it from the list and
        /// then adding it at the end.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="re"></param>
        private void FileRenamed(object source, RenamedEventArgs re)
        {
            this.log("File Renamed");
            this.RemovePhoto(re.OldFullPath);
            this.AddPhoto(re.FullPath);
        }

        /// <summary>
        /// Adds the new files in the current directory to the list of photos.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="fe"></param>
        private void FileCreated(object source, FileSystemEventArgs fe)
        {
            this.log("File Created");
            if(this.chkDynamic.Checked)
            {
                foreach (ListViewItem item in lstPictures.Items)
                {
                    if (item.SubItems[1].Text.Equals(fe.FullPath))
                    {
                        return;
                    }
                }

                this.AddPhoto(fe.FullPath);
                this.SetRecentPhoto(fe.FullPath);
            }
        }

        /// <summary>
        /// Trigger for FileChanged within the current folder.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="fe"></param>
        private void FileChanged(object source, FileSystemEventArgs fe)
        {
            //this.log("File Changed");
        }

        /// <summary>
        /// Remove the preview image by turning it into a blank image using a brush.
        /// </summary>
        private void RemovePreviewImage()
        {
            if(picPreview.Image != null)
            {
                
            }
        }

        /// <summary>
        /// Pulls up the folder selection to set the current folder.
        /// </summary>
        public void ShowFolderSelection()
        {
            DialogResult result = diagFolderSelector.ShowDialog();
            if (result == DialogResult.OK)
            {
                this.SetFolder(diagFolderSelector.SelectedPath);
            }
        }

        public void ShowFileSelection()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public int GetRandomPhotoIndex(int x = -1)
        {
            if (this.lstPictures.Items.Count <= 1)
                return 0;
            else
            {
                int i = 0;
                Random rnd = new Random();
                i = rnd.Next(0, lstPictures.Items.Count);
                if (x >= 0)
                {
                    while (i != x)
                    {
                        i = rnd.Next(0, lstPictures.Items.Count);
                    }
                }
                return i;
            }
        }

        /// <summary>
        /// Gets a random photo file name.
        /// </summary>
        /// <param name="x"></param>
        /// <returns>Random photo file name</returns>
        public string GetRandomPhoto(int x = -1)
        {
            if (this.lstPictures.Items.Count <= 0)
                return "";
            else if (this.lstPictures.Items.Count == 1)
                return this.lstPictures.Items[0].SubItems[1].Text;
            else
            {
                return this.lstPictures.Items[this.GetRandomPhotoIndex(x)].SubItems[1].Text;
            }
        }

        /// <summary>
        /// Processes the gallery.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void ProcessGallery(object sender, EventArgs args)
        {
            try
            {
                if(this.videoPlaying)
                {
                    //set interval
                    tmCurrentPhoto.Interval = 1000 * ((int)nmPhotoInterval.Value);

                    //reset if playing
                    if (playing)
                        tmCurrentPhoto.Start();

                    return;
                }

                //sponsor photos take priority
                if(this.SponsorPhotos.Count >= 1 && this.chkSponsors.Checked && !this.sponsorFlag)
                {
                    this.sponsorIndex++;
                    if (this.sponsorIndex >= this.SponsorPhotos.Count)
                        this.sponsorIndex = 0;
                    else if (this.sponsorIndex < 0)
                        this.sponsorIndex = this.SponsorPhotos.Count - 1;
                    
                    //set interval
                    tmCurrentPhoto.Interval = 1000 * ((int)nmPhotoInterval.Value);

                    if (this.strCurrentPicture == null || this.strNextPicture == null)
                    {
                        this.SetCurrentPhoto(this.SponsorPhotos[this.sponsorIndex]);
                        if (lstPictures.Items.Count > 1)
                        {
                            //set next photo if it exists, and ignore the current photo
                            string photo = this.GetRandomPhoto(this.photoIndex);
                            if (!photo.Equals(""))
                                this.SetNextPhoto(photo);
                        }
                    }
                    else
                    {
                        this.SetCurrentPhoto(strNextPicture);
                        this.SetNextPhoto(this.SponsorPhotos[this.sponsorIndex]);
                    }

                    //reset if playing
                    if (playing)
                        tmCurrentPhoto.Start();
                    this.sponsorFlag = true;
                    return;
                }

                this.sponsorFlag = false;

                //skip if we have no photos
                if(lstPictures.Items.Count == 1)
                {
                    this.photoIndex = 0;
                }
                else if(lstPictures.Items.Count > 1)
                {
                    if(chkRandom.Checked)
                    {
                        Random rnd = new Random();
                        this.photoIndex = rnd.Next(0, lstPictures.Items.Count);
                    }
                    else if(this.photoIndex >= lstPictures.Items.Count)
                    {
                        this.photoIndex = 0;
                    }
                    else
                    {
                        this.photoIndex++;
                    }
                }


                string nPicture = lstPictures.Items[photoIndex].SubItems[1].Text;

                //set current photo if not available.
                if (this.strCurrentPicture == null || this.strNextPicture == null)
                {
                    //Console.WriteLine("No photo yet...");
                    this.SetCurrentPhoto(nPicture);
                    if(lstPictures.Items.Count > 1)
                    {
                        //set next photo if it exists, and ignore the current photo
                        string photo = this.GetRandomPhoto(this.photoIndex);
                        if(!photo.Equals(""))
                            this.SetNextPhoto(photo);
                    }
                }
                else
                {
                    //show the next photo on the screen
                    //then queue the selected photo to the current photo
                    //Console.WriteLine("Moving photo through que...");
                    this.SetCurrentPhoto(strNextPicture);
                    this.SetNextPhoto(nPicture);
                }

                //set interval
                tmCurrentPhoto.Interval = 1000 * ((int) nmPhotoInterval.Value);

                //reset if playing
                if(playing)
                    tmCurrentPhoto.Start();
            }
            catch(Exception e)
            {
                Console.WriteLine("ProcessGallery: " + e.Message);
            }
        }

        /// <summary>
        /// Closes the program.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnExit_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        /// <summary>
        /// Sets the current folder, removing all current photos first.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSetFolder_Click(object sender, EventArgs e)
        {
            DialogResult result = diagFolderSelector.ShowDialog();
            if(result == DialogResult.OK)
            {
                this.SetFolder(diagFolderSelector.SelectedPath);
            }
        }

        /// <summary>
        /// Sets the next photo or the current photo in the gallery cycle.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lstPictures_ItemActivate(object sender, EventArgs e)
        {
            lstPictures.Items[lstPictures.SelectedIndices[0]].Checked = false;
            if (playing)
                this.SetNextPhoto(lstPictures.Items[lstPictures.SelectedIndices[0]].SubItems[1].Text);
            else
                this.SetCurrentPhoto(lstPictures.Items[lstPictures.SelectedIndices[0]].SubItems[1].Text);
        }

        /// <summary>
        /// Sets the preview picture when the index changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lstPictures_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(lstPictures.SelectedIndices.Count > 0)
                this.SetPreviewPhoto(lstPictures.Items[lstPictures.SelectedIndices[0]].SubItems[1].Text);
        }

        /// <summary>
        /// Removes all files from the list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClear_Click(object sender, EventArgs e)
        {
            this.reload();
        }

        /// <summary>
        /// Adds all photos in the selected folder.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAddFolder_Click(object sender, EventArgs e)
        {
            DialogResult result = diagFolderSelector.ShowDialog();
            if (result == DialogResult.OK)
            {
                this.AddPhotos(diagFolderSelector.SelectedPath);
            }
        }

        /// <summary>
        /// Adds a single file to the list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAddFile_Click(object sender, EventArgs e)
        {
            diagFileSelector.Filter = "Photos|*.jpg";
            DialogResult result = diagFileSelector.ShowDialog();
            if(result == DialogResult.OK)
            {
                this.AddPhoto(diagFileSelector.FileName);
            }
        }

        private void lstPictures_DragDrop(object sender, DragEventArgs e)
        {

        }

        /// <summary>
        /// Adds the monitors to the selection.
        /// </summary>
        private void LoadMonitors()
        {
            cbMonitors.Items.Clear();
            int total = 0;
            foreach(Screen screen in Screen.AllScreens)
            {
                string name = screen.DeviceName;
                name = name.Replace("\\\\.\\", "");
                if (screen.Primary)
                    name = "Primary Display";
                cbMonitors.Items.Add(name);
                total++;
            }

            cbMonitors.SelectedIndex = 0;
            if(total > 1)
            {
                cbMonitors.SelectedIndex = total - 1;
            }
        }

        /// <summary>
        /// Toggle if the folder automatically updates
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkDynamic_CheckedChanged(object sender, EventArgs e)
        {
            if(this.wtrFolder.Path != null && Directory.Exists(this.wtrFolder.Path))
                this.wtrFolder.EnableRaisingEvents = this.chkDynamic.Checked;
        }

        /// <summary>
        /// Reloads the list of files to view from.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRescan_Click(object sender, EventArgs e)
        {
            if (this.wtrFolder.Path != null && Directory.Exists(this.wtrFolder.Path))
            {
                this.SetFolder(this.wtrFolder.Path);
            }
        }

        /// <summary>
        /// Stops or Starts the slideshow.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStopPlay_Click(object sender, EventArgs e)
        {
            //stop playing video
            if (this.videoPlaying)
            {
                this.GalleryDisplay.stopVideo();
                this.videoPlaying = false;
                this.btnPlayVideo.Text = "Play Video";
            }

            //ignore if no pictures
            if (this.lstPictures.Items.Count <= 0)
                return;

            //show just the 1 picture
            if(this.lstPictures.Items.Count == 1)
            {
                this.SetCurrentPhoto(lstPictures.Items[0].SubItems[1].Text);
                return;
            }

            if (this.tmCurrentPhoto.Enabled)
                this.tmCurrentPhoto.Stop();

            this.initGallery();

            //this.playing = !this.playing;
            if (this.playing)
            {
                this.btnStopPlay.Text = "Play";
                this.tmCurrentPhoto.Stop();
            }
            else
            {
                if (this.picCurrent.Image == null || this.strCurrentPicture == null)
                {
                    this.photoIndex = 0;
                    this.SetCurrentPhoto(this.lstPictures.Items[this.photoIndex].SubItems[1].Text);
                    this.SetNextPhoto(this.GetRandomPhoto(1));
                }

                this.btnStopPlay.Text = "Stop";
                this.tmCurrentPhoto.Start();
                if (!this.GalleryDisplay.Visible)
                {
                    this.GalleryDisplay.Show();
                    this.btnShowHide.Text = "Hide";
                }
                this.GalleryDisplay.ResizePane();
            }
            this.playing = !this.playing;
        }

        /// <summary>
        /// Archives the current files and removes the original from
        /// the list and from disk.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnArchiveFiles_Click(object sender, EventArgs e)
        {
            if (this.lstPictures.Items.Count <= 0)
                return;

            //get all files that are jpegs
            List<string> files = new List<string>();
            foreach(ListViewItem item in this.lstPictures.Items)
            {
                if(this.isPhoto(item.SubItems[1].Text))
                    files.Add(item.SubItems[1].Text);
                //FileInfo file = new FileInfo(item.SubItems[1].Text);

                //switch (file.Extension.ToLower())
                //{
                //    case ".jpg":
                //    case ".gif":
                //    case ".png":
                //    case ".jpeg":
                //        files.Add(item.SubItems[1].Text);
                //        break;
                //}
            }

            //get selected folder
            DialogResult result = diagFolderSelector.ShowDialog();
            if(result == DialogResult.OK)
            {
                try
                {
                    //ignore if same folder
                    if (diagFolderSelector.SelectedPath.Equals(strCurrentFolder))
                        return;

                    //clear all photos
                    this.ClearCurrentPhoto();
                    this.ClearNextPhoto();
                    this.ClearPreviewPhoto();
                    this.ClearRecentPhoto();

                    //archive
                    ArchiveFiles(diagFolderSelector.SelectedPath, files);
                }
                catch(Exception ex)
                {
                    this.log("ArchiveFiles Exception: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Archives the given files to the given directory, and removes
        /// the originals. Each file in files should be a full path.
        /// 
        /// Additionally, avoids overwriting by append ###'s to the end of the file name.
        /// 
        /// </summary>
        /// <param name="dirName"></param>
        /// <param name="files"></param>
        private async void ArchiveFiles(string dirName, List<string> files)
        {
            foreach(string fileName in files)
            {
                FileInfo file = new FileInfo(fileName);
                this.RemovePhoto(fileName);
                try
                {
                    using (FileStream fs = File.Open(fileName, FileMode.Open))
                    {
                        string pname = dirName + "\\" + file.Name;
                        int i = 1;

                        //generate a new file extension
                        while(File.Exists(pname))
                        {
                            string num = i.ToString().PadLeft(3, '0');
                            pname = dirName + "\\" + Path.GetFileNameWithoutExtension(file.FullName) 
                                + "_" + num + file.Extension;
                            i++;
                        }

                        using (FileStream fw = File.Create(pname))
                        {
                            await fs.CopyToAsync(fw);
                        }
                    }

                    //delete photo
                    File.Delete(fileName);
                }
                catch(Exception e)
                {
                    this.log("ArchiveFiles: " + e.Message);
                }
            }
        }

        /// <summary>
        /// Detects when the value of the interval has changed for the gallery
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void nmPhotoInterval_ValueChanged(object sender, EventArgs e)
        {
            tmCurrentPhoto.Interval = 1000 * ((int)nmPhotoInterval.Value);
        }

        /// <summary>
        /// Deletes the recent, dynamically added photo.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDeleteRecent_Click(object sender, EventArgs e)
        {
            if (this.strRecentPhoto == null)
                return;

            if(File.Exists(this.strRecentPhoto))
            {
                string filename = this.strRecentPhoto;
                
                
                try
                {
                    //Console.WriteLine("Attempting to delete recent photo");
                    this.ClearRecentPhoto();
                    //Console.WriteLine("Removed");
                    File.Delete(filename);
                    //Console.WriteLine("Deleted");
                }
                catch(Exception ex)
                {

                }
            }
        }

        /// <summary>
        /// Deletes checked files
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDeleteFiles_Click(object sender, EventArgs e)
        {
            //ignore if no files
            if (this.lstPictures.Items.Count <= 0)
                return;
            
            List<ListViewItem> items = new List<ListViewItem>();

            foreach (ListViewItem item in this.lstPictures.Items)
            {
                if(item.Checked)
                {
                    items.Add((ListViewItem) item.Clone());
                }
            }

            if (items.Count <= 0)
                return;

            this.ClearPreviewPhoto();

            foreach(ListViewItem item in items)
            {
                try
                {
                    File.Delete(item.SubItems[1].Text);
                    this.RemovePhoto(item.SubItems[1].Text);
                }
                catch(Exception ex)
                {

                }
            }
        }

        /// <summary>
        /// Initializes the gallery view form.
        /// </summary>
        private void initGallery()
        {
            if (this.GalleryDisplay == null || this.GalleryDisplay.IsDisposed)
            {
                this.GalleryDisplay = new PhotoDisplay();
                this.GalleryDisplay.FormClosed += GalleryActions;
                this.GalleryDisplay.Activated += GalleryActions;
                this.GalleryDisplay.Disposed += GalleryActions;
                this.GalleryDisplay.VisibleChanged += GalleryActions;


                AxWMPLib.AxWindowsMediaPlayer player = this.GalleryDisplay.getPlayer();
                player.PlayStateChange += this.GalleryVideoActions;
            }

            int index = this.cbMonitors.SelectedIndex;
            int x = 0;
            int y = 0;
            int w = 0;
            int h = 0;

            if (Screen.AllScreens.Length <= 1)
            {
                w = Screen.AllScreens[0].Bounds.Width;
                h = Screen.AllScreens[0].Bounds.Height;
            }
            else if (Screen.AllScreens[index] != null)
            {
                w = Screen.AllScreens[index].Bounds.Width;
                h = Screen.AllScreens[index].Bounds.Height;
                x = Screen.AllScreens[index].Bounds.X;
                y = Screen.AllScreens[index].Bounds.Y;
            }

            this.GalleryDisplay.SetBounds(x, y, w, h);
            this.GalleryDisplay.WindowState = FormWindowState.Maximized;
            this.GalleryDisplay.FormBorderStyle = FormBorderStyle.None;
            this.GalleryDisplay.ResizePane();
            if (this.strCurrentPicture != null)
            {
                this.GalleryDisplay.SetPhoto(this.strCurrentPicture);
            }
        }

        /// <summary>
        /// Listener for when the video state changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GalleryVideoActions(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {
            try
            {
                int state = e.newState;
                switch(state)
                {
                    case 1:
                    case 2:
                        this.videoPlaying = false;
                        this.btnPlayVideo.Text = "Play Video";
                        break;
                        
                    //done playing (finished)
                    case 8:
                        this.videoPlaying = false;
                        this.btnPlayVideo.Text = "Play Video";
                        if (!this.tmCurrentPhoto.Enabled)
                        {
                            tmCurrentPhoto.Start();
                        }
                        break;

                    default:
                        this.videoPlaying = true;
                        if (this.tmCurrentPhoto.Enabled)
                            this.tmCurrentPhoto.Stop();
                        this.GalleryDisplay.Show();
                        this.btnShowHide.Text = "Hide";
                        //this.GalleryDisplay.getPlayer().Show();
                        //this.GalleryDisplay.getPlayer().
                        this.btnPlayVideo.Text = "Stop Video";
                        break;
                }
            }
            catch(Exception ex)
            {

            }
        }

        /// <summary>
        /// Shows or Hides the main window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnShowHide_Click(object sender, EventArgs e)
        {
            this.initGallery();
            if (this.GalleryDisplay.Visible)
            {
                this.GalleryDisplay.Hide();
                this.btnShowHide.Text = "Show";
            }
            else
            {
                this.GalleryDisplay.Show();
                this.btnShowHide.Text = "Hide";
            }
        }

        /// <summary>
        /// Detects when the window for viewing photos has been changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ea"></param>
        public void GalleryActions(object sender, EventArgs ea)
        {
            if(this.GalleryDisplay == null || this.GalleryDisplay.IsDisposed)
            {
                this.btnShowHide.Text = "Show";
            }
            else
            {
                if (this.GalleryDisplay.Visible)
                    this.btnShowHide.Text = "Hide";
                else
                    this.btnShowHide.Text = "Show";
            }
        }

        /// <summary>
        /// Hides the preview picture
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="me"></param>
        public void HidePreviewPicture(object sender, EventArgs ea)
        {
            //this.picView.Hide();
            //if (this.picView.Image != null)
                //this.picView.Image.Dispose();
            //this.picView.Image = bmpBlank;
        }

        /// <summary>
        /// Displays the object's image on the preview, over the list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ea"></param>
        public void ShowPreviewPicture(object sender, EventArgs ea)
        {
            //Console.WriteLine(sender.GetType());
            //Console.WriteLine(typeof(PictureBox));
            if(sender.GetType() == typeof(PictureBox))
            {
                PictureBox box = (PictureBox)sender;
                //if(box.Image != null && box.Image != bmpBlank)
                {
                    //Console.WriteLine("OK...");
                    //if(this.picView.Image != null)
                    //    this.picView.Image.Dispose();
                    //this.picView.ImageLocation = box.ImageLocation;
                }
                
                //setup preview window
                if(this.PhotoPreview == null || this.PhotoPreview.IsDisposed)
                    this.PhotoPreview = new PhotoDisplay();
                this.PhotoPreview.StartPosition = FormStartPosition.CenterScreen;
                this.PhotoPreview.FormBorderStyle = FormBorderStyle.FixedToolWindow;
                this.PhotoPreview.Show();
                this.PhotoPreview.Focus();
                this.PhotoPreview.Disposed += this.PreviewPictureEvents;
                this.PhotoPreview.SetPhoto((Bitmap) box.Image.Clone());
                this.Enabled = false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ea"></param>
        public void PreviewPictureEvents(object sender, EventArgs ea)
        {
            this.Enabled = true;
            if(this.GalleryDisplay != null && this.GalleryDisplay.Bounds == this.Bounds
                && this.GalleryDisplay.Visible)
            {
                this.GalleryDisplay.Focus();
            }
            else
            {
                this.Focus();
            }
        }

        /// <summary>
        /// Selects all items in the list (or deselects all if all are checked)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            if (this.lstPictures.Items.Count <= 0)
                return;

            bool allChecked = true;
            foreach(ListViewItem item in this.lstPictures.Items)
            {
                if (!item.Checked)
                    allChecked = false;
                item.Checked = true;
                string name = item.SubItems[1].Text;
                if(name.Equals(strCurrentPicture) || name.Equals(strPreviewPicture) || name.Equals(strNextPicture))
                {
                    item.Checked = false;
                }
            }

            if(allChecked)
            {
                foreach (ListViewItem item in this.lstPictures.Items)
                {
                    item.Checked = false;
                }
            }
        }

        /// <summary>
        /// Shows a dialog for selecting the sponsor folder.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSponsorFolder_Click(object sender, EventArgs e)
        {
            DialogResult result = diagFolderSelector.ShowDialog();
            if (result == DialogResult.OK)
            {
                this.SetSponsorFolder(diagFolderSelector.SelectedPath);
            }
        }

        /// <summary>
        /// Shows a dialog for selecting a video and playing it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPlayVideo_Click(object sender, EventArgs e)
        {

            if(this.videoPlaying)
            {
                try
                {
                    this.GalleryDisplay.getPlayer().Ctlcontrols.stop();
                }
                catch(Exception ex)
                {

                }
            }
            else
            {

                diagFileSelector.Filter = "Videos|*.mp4;*.mpeg;*.wmv";
                DialogResult result = this.diagFileSelector.ShowDialog();
                if (result == DialogResult.OK)
                {
                    this.GalleryDisplay.PlayVideo(this.diagFileSelector.FileName);
                }
            }
        }
    }
}
