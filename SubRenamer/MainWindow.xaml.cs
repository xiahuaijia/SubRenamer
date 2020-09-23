using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using NLog;
using SubRenamer.Annotations;

namespace SubRenamer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private GridViewColumn Column { get; }
        public ModelList ModelList { get; } = new ModelList();
        private bool _eatSushi;
        private bool _copySub;
        private bool _GbToBig5;
        private bool _Big5ToGb;
        private bool _IsMoveSubTime;


        private Logger Logger { get; } = LogManager.GetCurrentClassLogger();
        private Logger SushiLogger { get; } = LogManager.GetLogger("Sushi");

        public bool CopySub
        {
            get => _copySub;
            set
            {
                _copySub = value;
                OnPropertyChanged();
            }
        }

        public bool EatSushi
        {
            get => _eatSushi;
            set
            {
                _eatSushi = value;
                if (_eatSushi)
                {
                    GridView.Columns.Insert(0, Column);
                }
                else
                {
                    GridView.Columns.RemoveAt(0);
                }
                OnPropertyChanged();
            }
        }

        public bool GbToBig5
        {
            get { return _GbToBig5; }
            set
            {
                _GbToBig5 = value;
                _Big5ToGb = !_GbToBig5;

                OnPropertyChanged("GbToBig5");
                OnPropertyChanged("Big5ToGb");
            }
        }

        public bool Big5ToGb
        {
            get { return _Big5ToGb; }
            set
            {
                _Big5ToGb = value;
                _GbToBig5 = !_Big5ToGb;

                OnPropertyChanged("GbToBig5");
                OnPropertyChanged("Big5ToGb");
            }
        }

        public bool IsMoveSubTime
        {
            get { return _IsMoveSubTime; }
            set
            {
                _IsMoveSubTime = value;
                OnPropertyChanged("IsMoveSubTime");
            }
        }

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
            Column = GridView.Columns[0];
            GridView.Columns.RemoveAt(0);
        }

        /// <summary>
        /// 选择原始视频（用于sushi自动调轴）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSelectOriginalMovie_OnClick(object sender, RoutedEventArgs e)
        {
            var selectFile = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "视频文件(*.mp4;*.mkv;*.m2ts)|*.mp4;*.mkv;*.m2ts|所有文件 (*.*)|*.*"
            };
            if (selectFile.ShowDialog() == true)
            {
                ModelList.AddOriginalMovie(selectFile.FileNames);
            }
        }

        /// <summary>
        /// 选择视频
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSelectMovie_OnClick(object sender, RoutedEventArgs e)
        {
            var selectFile = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "视频文件(*.mp4;*.mkv;*.m2ts)|*.mp4;*.mkv;*.m2ts|所有文件 (*.*)|*.*"
            };
            if (selectFile.ShowDialog() == true)
            {
                ModelList.AddMovie(selectFile.FileNames);
            }
        }

        /// <summary>
        /// 选择字幕
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSelectSub_OnClick(object sender, RoutedEventArgs e)
        {
            var selectFile = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "字幕文件(*.ass;*.ssa;*.srt)|*.ass;*.ssa;*.srt|所有文件 (*.*)|*.*"
            };
            if (selectFile.ShowDialog() == true)
            {
                ModelList.AddSub(selectFile.FileNames);
            }
        }

        /// <summary>
        /// 执行重命名
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnRename_OnClick(object sender, RoutedEventArgs e)
        {
            ProgressDialogController controller = null;
            if (EatSushi)
            {
                controller = await this.ShowProgressAsync("正在处理", "正在处理第1个字幕");
                controller.Minimum = 0;
                controller.Maximum = 1;
            }
            var sb = new StringBuilder();
            for (var i = 0; i < ModelList.Models.Count; i++)
            {
                if (EatSushi) controller?.SetMessage($"正在处理第{i + 1}个字幕");
                Logger.Info($"正在处理第{i + 1}个字幕");
                var model = ModelList.Models[i];

                var file_name = Path.GetFileName(model.SubFileName);
                try
                {
                    //创建重命名文件
                    if (!string.IsNullOrEmpty(model.MovieFileName))
                    {
                        model.GenerateRenameSubFiles(CopySub);

                        //sushi自动调轴
                        for (var j = 0; j < model.SubFiles.Count; j++)
                        {
                            if (!EatSushi)
                            {
                                if (CopySub) model.SubFiles[j].CopyTo(model.RenamedSubFiles[j].FullName);
                                else model.SubFiles[j].MoveTo(model.RenamedSubFiles[j].FullName);
                            }
                            else
                            {

                                //1、文件必须是UTF-8
                                //2、只支持srt或者ass格式
                                var process = new Process
                                {
                                    StartInfo = new ProcessStartInfo
                                    {
                                        FileName = Path.Combine("Sushi", "sushi.exe"),
                                        Arguments = $"--src \"{model.OriginalMovieFile.FullName}\" " +
                                                    $"--dst \"{model.MovieFile.FullName}\" " +
                                                    $"--script \"{model.SubFiles[j].FullName}\" " +
                                                    $"-o \"{model.RenamedSubFiles[j].FullName}\"",
                                        UseShellExecute = false,
                                        CreateNoWindow = true,
                                        //RedirectStandardOutput = true,
                                        //RedirectStandardError = true
                                    }
                                };
                                //process.OutputDataReceived += (senderx, ex) => SushiLogger.Info(ex.Data);
                                //process.ErrorDataReceived += (senderx, ex) => SushiLogger.Info(ex.Data);
                                process.Start();
                                await Task.Run(() => process.WaitForExit());
                                if (!CopySub) model.SubFiles[j].Delete();
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("请选择对应的原视频。");
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"第{i + 1}个字幕出现错误：{ex.Message}");
                    Logger.Error(ex, $"第{i + 1}个字幕出现错误：字幕名称【{file_name}】，错误信息【{ex.Message}】");
                }
                if (EatSushi) controller?.SetProgress((i + 1d) / ModelList.Models.Count);
            }
            if (EatSushi && controller != null) await controller.CloseAsync();
            Logger.Info("重命名完成");
            var message = sb.ToString();
            if (string.IsNullOrWhiteSpace(message))
            {
                await this.ShowMessageAsync("成功", "重命名成功");
            }
            else
            {
                await this.ShowMessageAsync("错误", message);
            }
            ModelList.Models.Clear();
        }

        /// <summary>
        /// 简繁体转换
        /// </summary>
        /// <param name="model"></param>
        /// <param name="file_path"></param>
        private void TransformText(Model model, string file_path, Encoding encoding)
        {
            var all_text = File.ReadAllText(file_path, Encoding.Default);
            if (GbToBig5)
            {
                all_text = model.Gb_Big5_transform(all_text);
            }
            else
            {
                all_text = model.Big5_Gb_Transform(all_text);
            }

            File.WriteAllText(file_path, all_text, encoding);
        }

        /// <summary>
        /// 清空列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnClearList_OnClick(object sender, RoutedEventArgs e)
        {
            ModelList.Models.Clear();
        }

        /// <summary>
        /// 界面表格拖动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListInfo_OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
                ModelList.AddDropFiles(files, EatSushi);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 简繁体转换
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnTransform_Click(object sender, RoutedEventArgs e)
        {
            if (!GbToBig5 && !Big5ToGb)
            {
                MessageBox.Show("请选择简繁体转换类型。");
                return;
            }

            var str_encoded = this.cbtoutf.Text.ToString();
            Encoding encoding = Encoding.Default;
            if (str_encoded.Contains("ANSI"))
            {
                encoding = Encoding.Default;
            }
            else if (str_encoded.Contains("UTF-8"))
            {
                encoding = Encoding.UTF8;
            }
            else if (str_encoded.Contains("GB2312"))
            {
                encoding = Encoding.GetEncoding("GB2312");
            }

            var sb = new StringBuilder();
            for (var i = 0; i < ModelList.Models.Count; i++)
            {
                var model = ModelList.Models[i];
                var file_name = Path.GetFileName(model.SubFileName);
                try
                {
                    //使用新路径进行，简繁体转换
                    for (int k = 0; k < model.SubFiles.Count; k++)
                    {
                        TransformText(model, model.SubFiles[k].FullName, encoding);
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"第{i + 1}个字幕出现错误：{ex.Message}");
                    Logger.Error(ex, $"第{i + 1}个字幕出现错误：字幕名称【{file_name}】，错误信息【{ex.Message}】");
                }
            }

            var message = sb.ToString();
            if (string.IsNullOrWhiteSpace(message))
            {
                await this.ShowMessageAsync("成功", "简繁体转换成功");
            }
            else
            {
                await this.ShowMessageAsync("错误", message);
            }
            ModelList.Models.Clear();
        }

        /// <summary>
        /// 批量转换
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnMeasurementConverter_Click(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SubtitleEdit/SubtitleEdit.exe");
            info.Arguments = "-b";
            info.WindowStyle = ProcessWindowStyle.Normal;
            Process pro = Process.Start(info);
            pro.WaitForExit();
        }
    }
}
