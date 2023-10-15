using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms; // Add this for the FolderBrowserDialog
using System.IO;
using System.Management.Automation;
using System.Windows.Media.Animation;

namespace RAMDrive_Runner
{
    //Class for managing Name and Size of subdirectories of selected Games folder
    public class FolderDetail
    {
        public string Name { get; set; }
        public string Size { get; set; } // for simplicity, this is a string; you might wish to calculate it differently
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            sourceDirectoryTextBox.ToolTip = sourceDirectoryTextBox.Text;
            UpdateFolderList();
            ramSizeLabel.Content = $"RAM Disk Size: {ramAllocationSlider.Value}GB";
        }

        private void SourceDirectoryTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dialog = new FolderBrowserDialog
            {
                SelectedPath = sourceDirectoryTextBox.Text  // Use the current text in the TextBox as the starting directory
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                sourceDirectoryTextBox.Text = dialog.SelectedPath; // Ensure your TextBox is named "sourceDirectoryTextBox" in your XAML.
                sourceDirectoryTextBox.ToolTip = dialog.SelectedPath;
                UpdateFolderList();
            }
        }
        private void UpdateFolderList()
        {
            string directoryPath = sourceDirectoryTextBox.Text;

            // Ensure the path exists
            if (!System.IO.Directory.Exists(directoryPath))
                return;

            var directories = System.IO.Directory.GetDirectories(directoryPath);
            var folderDetails = directories.Select(dir => new FolderDetail
            {
                Name = System.IO.Path.GetFileName(dir),
                Size = GetFolderSize(dir) // this would be another method you implement
            }).ToList();

            folderList.DataContext = folderDetails;
        }

        private string GetFolderSize(string folderPath)
        {
            DirectoryInfo dir = new DirectoryInfo(folderPath);

            // Use recursion to sum up sizes of all files in the directory and its subdirectories
            long totalSizeInBytes = dir.GetFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);

            // Convert to more readable format (e.g., MB, GB)
            return FormatBytesToReadableSize(totalSizeInBytes);
        }

        private string FormatBytesToReadableSize(long bytes)
        {
            const int byteConversion = 1024;
            double bytesAsDouble = bytes;

            if (bytes >= Math.Pow(byteConversion, 3)) // GB Range
                return string.Format("{0:0.##} GB", bytesAsDouble / Math.Pow(byteConversion, 3));
            else if (bytes >= Math.Pow(byteConversion, 2)) // MB Range
                return string.Format("{0:0.##} MB", bytesAsDouble / Math.Pow(byteConversion, 2));
            else if (bytes >= byteConversion) // KB Range
                return string.Format("{0:0.##} KB", bytesAsDouble / byteConversion);
            else // Bytes
                return string.Format("{0} Bytes", bytes);
        }
        private void FolderList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (folderList.SelectedItem is FolderDetail selectedFolder)
            {
                // Convert the Size string to its numeric value (removing GB/MB/KB/Bytes).
                // For this example, I assume you want to set the value in GB. Adjust as needed.
                var sizeString = selectedFolder.Size;
                var sizeValue = ConvertToGB(sizeString);
                ramAllocationSlider.Value = sizeValue;
            }
        }

        private double ConvertToGB(string size)
        {
            double sizeValue;

            if (size.EndsWith(" GB"))
                sizeValue = double.Parse(size.Replace(" GB", ""));
            else if (size.EndsWith(" MB"))
                sizeValue = double.Parse(size.Replace(" MB", "")) / 1024; // Convert MB to GB
            else if (size.EndsWith(" KB"))
                sizeValue = double.Parse(size.Replace(" KB", "")) / (1024 * 1024); // Convert KB to GB
            else // Bytes
                sizeValue = double.Parse(size.Replace(" Bytes", "")) / (1024 * 1024 * 1024); // Convert Bytes to GB

            // Multiply by 1.05 and round up
            sizeValue *= 1.05;
            sizeValue = Math.Ceiling(sizeValue);

            return sizeValue;
        }
        private void ramAllocationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ramSizeLabel.Content = $"RAM Disk Size: {ramAllocationSlider.Value}GB";
        }
        private async void OnMountCopyLink(object sender, RoutedEventArgs e)
        {
            // Show overlay and start spinner animation on the main UI thread
            overlayGrid.Visibility = Visibility.Visible;
            RotateSpinner();

            string sourceDirectory = string.Empty;
            FolderDetail selectedFolder = null;
            double ramDiskSizeValue = 0;

            // Access UI elements on the main UI thread before starting the background task
            Dispatcher.Invoke(() =>
            {
                sourceDirectory = sourceDirectoryTextBox.Text;
                selectedFolder = (FolderDetail)folderList.SelectedItem;
                ramDiskSizeValue = ramAllocationSlider.Value;
            });

            if (selectedFolder == null)
            {
                // Show user error message on the main UI thread
                Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show("No folder selected. Please select a folder before proceeding.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });

                // Hide overlay on the main UI thread
                overlayGrid.Visibility = Visibility.Collapsed;
                return;
            }

            await Task.Run(() =>
            {
                string selectedFolderPath = System.IO.Path.Combine(sourceDirectory, selectedFolder.Name);
                string ramDiskSize = Math.Ceiling(ramDiskSizeValue * 1024).ToString(); // Convert GB to MB
                char driveLetter = 'Z'; // Consider making this dynamic or let the user choose.

                // Mount RAMDisk
                string command = $"imdisk -a -s {ramDiskSize}M -m {driveLetter}:";
                ExecutePowerShellCommand(command);

                // Format the RAMDisk with NTFS
                command = $"format {driveLetter}: /FS:NTFS /Q /Y"; // /Q for quick format, /Y to suppress user prompts
                ExecutePowerShellCommand(command);

                // Copy folder to RAMDisk
                command = $"Copy-Item -Path '{selectedFolderPath}' -Destination '{driveLetter}:' -Recurse";
                ExecutePowerShellCommand(command);

                // Rename original folder
                string renamedFolderPath = selectedFolderPath + "_RAMDisk";
                command = $"Rename-Item -Path '{selectedFolderPath}' -NewName '{System.IO.Path.GetFileName(renamedFolderPath)}'";
                ExecutePowerShellCommand(command);

                // Create junction link
                command = $"New-Item -ItemType Junction -Path '{selectedFolderPath}' -Value '{driveLetter}:\\{System.IO.Path.GetFileName(selectedFolderPath)}'";
                ExecutePowerShellCommand(command);

                // swap the enablement of the mount and unmount buttons if there are no errors after execution.
                // This needs to be done on the main UI thread.
                Dispatcher.Invoke(() =>
                {
                    if (ErrorsAfterExecution() == 0)
                    {
                        unmountRestoreButton.IsEnabled = true;
                        mountLinkButton.IsEnabled = false;
                        // Disable the folderList so user cannot select another folder
                        folderList.IsEnabled = false;
                    }
                });
            });

            // Hide overlay on the main UI thread
            overlayGrid.Visibility = Visibility.Collapsed;
        }

        private async void OnUnmountRestore(object sender, RoutedEventArgs e)
        {
            // Show overlay
            overlayGrid.Visibility = Visibility.Visible;
            RotateSpinner();  // Assuming you have this function for the spinner rotation

            await Task.Run(() =>
            {
                string sourceDirectory = string.Empty;
                FolderDetail selectedFolder = null;

                // Accessing UI elements from the dispatcher thread
                Dispatcher.Invoke(() =>
                {
                    sourceDirectory = sourceDirectoryTextBox.Text;
                    selectedFolder = (FolderDetail)folderList.SelectedItem;
                });

                if (selectedFolder == null)
                {
                    // Show user error message on the main UI thread
                    Dispatcher.Invoke(() =>
                    {
                        System.Windows.MessageBox.Show("No folder selected. Please select a folder before proceeding.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        overlayGrid.Visibility = Visibility.Collapsed;  // Hide overlay
                    });
                    return;
                }

                string selectedFolderPath = System.IO.Path.Combine(sourceDirectory, selectedFolder.Name);
                char driveLetter = 'Z'; // As before

                // Remove the junction link
                string command = $"Remove-Item '{selectedFolderPath}'";
                ExecutePowerShellCommand(command);

                // Restore the original folder name
                string renamedFolderPath = selectedFolderPath + "_RAMDisk";
                command = $"Rename-Item -Path '{renamedFolderPath}' -NewName '{System.IO.Path.GetFileName(selectedFolderPath)}'";
                ExecutePowerShellCommand(command);

                // Unmount RAMDisk
                command = $"imdisk -D -m {driveLetter}:";
                ExecutePowerShellCommand(command);

                // swap enablement of mount and unmount after execution using the dispatcher
                if (ErrorsAfterExecution() == 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        unmountRestoreButton.IsEnabled = false;
                        mountLinkButton.IsEnabled = true;
                        // enable the folderList so user cannot select another folder
                        folderList.IsEnabled = true;
                    });
                }
            });

            // Hide overlay
            overlayGrid.Visibility = Visibility.Collapsed;
        }

        private void ExecutePowerShellCommand(string command)
        {
            using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create())
            {
                ps.AddScript(command);

                // Redirect errors to process them and pass the command string
                ps.Streams.Error.DataAdded += (sender, e) => Error_DataAdded(sender, e, command);

                // Execute the command
                ps.Invoke();

                // Handle any errors
                if (ps.Streams.Error.Count > 0)
                {
                    StringBuilder errorMessage = new StringBuilder("Errors encountered while executing command:\n");
                    errorMessage.AppendLine($"\"{command}\"\n");  // Append the command that caused the error

                    foreach (var errorRecord in ps.Streams.Error)
                    {
                        errorMessage.AppendLine(errorRecord.ToString());
                    }

                    System.Windows.MessageBox.Show(errorMessage.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Event handler to collect errors
        private void Error_DataAdded(object sender, System.Management.Automation.DataAddedEventArgs e, string command)
        {
            // Retrieve the error record
            var errorRecord = (sender as System.Management.Automation.PSDataCollection<System.Management.Automation.ErrorRecord>)[e.Index];

            // Formulate the error message with date, timestamp, and the command
            string errorMessage = $"[{DateTime.Now}] - Command: \"{command}\" - Error: {errorRecord.ToString()}\n";

            // Define the log file path
            string logFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RAMRunnerErrorLog.txt");

            // Append the error message to the log file
            File.AppendAllText(logFilePath, errorMessage);
        }

        private int ErrorsAfterExecution()
        {
            using (System.Management.Automation.PowerShell psCheck = System.Management.Automation.PowerShell.Create())
            {
                // Check for any errors in the last command
                return psCheck.Streams.Error.Count;
            }
        }
        private void RotateSpinner()
        {
            DoubleAnimation da = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = new Duration(TimeSpan.FromSeconds(2)),
                RepeatBehavior = RepeatBehavior.Forever
            };
            RotateTransform rt = new RotateTransform();
            spinner.RenderTransform = rt;
            spinner.RenderTransformOrigin = new Point(0.5, 0.5);
            rt.BeginAnimation(RotateTransform.AngleProperty, da);
        }

    }
}
