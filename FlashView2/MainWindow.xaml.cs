using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
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
using Microsoft.Win32;

namespace FlashView2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public byte[]? FlashFile { get; set; }
        public ApplicationViewModel AppViewModel { get; set; }
        
        public MainWindow()
        {
            InitializeComponent();          
        }

        private void MenuItemOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Flash Files|*.fl";
            openFileDialog.Title = "Выберите flash-файл с данными";
            List<List<string>> dataConfig = new List<List<string>>();
            if (openFileDialog.ShowDialog() == true)
            {
                string pathFlash = openFileDialog.FileName;
                try
                {
                    // считываем данные флеш-файла
                    FlashFile = File.ReadAllBytes(pathFlash);
                    FlashFile = FlashFile.Skip(384).ToArray();
                    // считываем данные конфиг-файла
                    string pathConfig = "Configurations\\flashRead_NNGK_v1_07-12-2022.cfg";
                    char[] separators = { ' ', '\t' };
                    using (var reader = new StreamReader(pathConfig))
                    {
                        while (!reader.EndOfStream)
                        {
                            var row = reader.ReadLine();

                            if (!string.IsNullOrWhiteSpace(row))
                            {
                                string[] line = row.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                                dataConfig.Add(new List<string>(line));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Возникла ошибка:" + ex.Message);
                    return;
                }
            }
            else
            {
                return;
            }

            List<Packet> packets = HandleConfigData(dataConfig);
            AppViewModel = new ApplicationViewModel(FlashFile, packets);            
            DataContext = AppViewModel;            
        }

        List<Packet> HandleConfigData(List<List<string>> dataConfig)
        {
            // расфасовываем данные нужным образом          
            string idDevice = "";
            string idPacket = "";
            string version = dataConfig[0][0];
            List<List<string>> DeviceParam = new(); // переработанный массив для выгрузки данных

            for (int i = 1; i < dataConfig.Count; i++)
            {
                for (int j = 0; j < dataConfig[i].Count; j++)
                {

                    if (dataConfig[i][0][0] == '@' && dataConfig[i + 2][0][0] == '@') // вычисляем id устройства, обрамленное @
                    {
                        idDevice = dataConfig[i + 1][0];
                        i = i + 2;
                        break;
                    }

                    if (dataConfig[i][j][0] == '~' && idDevice != "") // вычисляем id пакета
                    {
                        idPacket = dataConfig[i][j].Trim('~');
                        do
                        {
                            i++;
                            List<string> data = new();
                            data.Add(idPacket);
                            data.Add(idDevice);
                            if (dataConfig[i][0][0] == '*')
                            {
                                dataConfig[i].RemoveAt(0);
                            }
                            else
                            {
                                throw new Exception("Ошибка описания строки данных flash");                                
                            }
                            data.AddRange(dataConfig[i]);
                            DeviceParam.Add(new List<string>(data));
                            if (dataConfig[i + 1][0][0] == '#') // записываем конец строки, удаляем лишние символы
                            {
                                dataConfig[i + 1].RemoveAll(x => x == "#" || x == "H" || x == "h"); // удаляем лишние элементы из Листа
                                List<string> str = dataConfig[i + 1];
                                DeviceParam.Add(new List<string>(str));
                                break;
                            }
                        } while (true);
                        i++;
                    }
                }
            }

            // далее идет переработка фассованных данных и создание пакетов
            List<Packet> PacketsSettings = new();

            for (int i = 0; i < DeviceParam.Count; i++)
            {
                var packet = new Packet();

                try
                {
                    packet.ID_Packet = byte.Parse(DeviceParam[i][0]);
                    packet.ID_Device = byte.Parse(DeviceParam[i][1]);
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.Message);
                    throw new Exception(ex.Message);
                }

                do
                {
                    var list = DeviceParam[i];
                    byte length = list[2] switch
                    {
                        "byteUs" => 1,
                        "byteS" => 1,
                        "shortUs" => 2,
                        "shortS" => 2,
                        "intS" => 4,
                        "intUs" => 4,
                        "bdTime" => 6,
                        _ => 0
                    };
                    if (length == 0)
                    {                        
                        throw new Exception("Неопознанное обозначение типа данных в конф.файле");
                    }
                    packet.LengthLine += length;
                    packet.LengthParams.Add(length);
                    packet.TypeParams.Add(list[2]);
                    if (list[5] == "[]")
                    {
                        packet.HeaderColumns.Add(list[4]);
                    }
                    else
                    {
                        packet.HeaderColumns.Add($"{list[4]} {list[5]}");
                    }

                    // пропускаем значение неопределенности
                    packet.TypeCalculate.Add(list[7]);
                    double[] data = new double[4];

                    for (int j = 8; j <= 11; j++)
                    {
                        bool isParseDouble = double.TryParse(list[j], NumberStyles.Any, CultureInfo.InvariantCulture, out double value);
                        if (!isParseDouble)
                        {                            
                            MessageBox.Show("Ошибка парсинга чисел для пересчета данного");                            
                        }
                        data[j - 8] = value;
                    }
                    packet.DataCalculation.Add(data); // загоняем коэффициенты для пересчета
                    bool isCountWidth = byte.TryParse(list[12], out byte resultCount);
                    bool isParseWidth = byte.TryParse(list[13], out byte resultWindth);

                    if (!isParseWidth && !isCountWidth)
                    {                        
                        throw new Exception("Ошибка парсинга чисел для пересчета данного");
                    }
                    packet.CountSign.Add(resultCount);
                    packet.WidthColumn.Add(resultWindth);
                    i++;
                } while (DeviceParam[i].Count != 2 && DeviceParam[i].Count != 0);

                if (DeviceParam[i].Count == 2)
                {
                    try
                    {
                        byte one = byte.Parse(DeviceParam[i][0]);
                        byte two = byte.Parse(DeviceParam[i][1]);
                        packet.endLine[0] = one;
                        packet.endLine[1] = two;
                    }
                    catch (Exception ex)
                    {                        
                        throw new Exception("Не удалось сконвертировать конец строки в байты.\n" + ex.Message);
                    }

                }
                else if (DeviceParam[i].Count == 0)
                {
                    packet.endLine[0] = 0;
                    packet.endLine[0] = 0;
                }               
                PacketsSettings.Add(packet);
            }
            
            return PacketsSettings;
        }

        void MenuItemCloseProgram_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        void r2_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {            
            if (e.PropertyName.Contains('[') || e.PropertyName.Contains(']') && e.Column is DataGridBoundColumn)
            {
                DataGridBoundColumn dataGridBoundColumn = e.Column as DataGridBoundColumn;
                dataGridBoundColumn.Binding = new Binding("[" + e.PropertyName + "]");
            }     

        }
    }
}
