using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Wordprocessing;
using FlashView2;
using Microsoft.Win32;
using System.Windows;
using FlashView2.Model;
using System.Data;
using System.Globalization;

namespace FlashView2
{
    internal class UnUsedCode
    {
        // метод обработки данных и формирования LAS
        //private SortedDictionary<double, double> GetDataForLasType2(List<(double, DateTime)> listDepthDate)
        //{
        //    SortedDictionary<double, double> depthAndKp = new SortedDictionary<double, double>();
        //    var choisedCoef = MyCalibrFile.CoefsCalibr[MyCalibrFile.CurrentChoise];
        //    int savePos = 0;
        //    TimeSpan timeSpan = TimeSpan.FromSeconds(3);

        //    if (IsSetInterval)
        //    {
        //        if (StartTimeRead > EndTimeRead) throw new ArgumentException("Неверно указан период даты считывания");
        //    }

        //    for (int i = 0; i < dtf.Data.Count; i++)
        //    {
        //        bool isTimeDepth = DateTime.TryParse(dtf.Data[i][dtf.ColumnDate], out DateTime timeDepth); // берем время с глубины время
        //        if (IsSetInterval)
        //        {
        //            if (timeDepth < StartTimeRead || timeDepth > EndTimeRead) continue;
        //        }
        //        if (!isTimeDepth) continue;

        //        double KP = -999; // коэф. по умолчанию

        //        for (int j = savePos; j < DataRowAVM.Count; j++)
        //        {
        //            DataRow row = DataRowAVM[j];
        //            var isTimeFlash = DateTime.TryParse(row["[Время/Дата]"].ToString(), out DateTime timeFlash); // берем время с флешки

        //            if (!isTimeFlash)
        //            {
        //                continue;
        //            }

        //            if (timeDepth >= timeFlash - timeSpan && timeDepth < timeFlash + timeSpan)
        //            {
        //                // вычисляем МЗ и БЗ и находим КП

        //                double mz = double.Parse(row["[ННК1/\nННК1(вода)]"].ToString()) / 333;
        //                double bz = double.Parse(row["[ННК2/\nННК2(вода)]"].ToString()) / 33;
        //                double x;



        //                if (bz != 0)
        //                {
        //                    x = mz / bz;
        //                    if (choisedCoef.Length == 2)
        //                    {
        //                        KP = choisedCoef[0] * x + choisedCoef[1];
        //                    }
        //                    else if (choisedCoef.Length == 3)
        //                    {
        //                        KP = choisedCoef[0] * x * x + choisedCoef[1] * x + choisedCoef[2];
        //                    }
        //                    double valueDepth1 = double.Parse(dtf.Data[i][2].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture);
        //                    depthAndKp.TryAdd(valueDepth1, KP);
        //                }

        //                savePos = j;
        //                break;
        //            }

        //            else if (timeFlash + timeSpan > timeDepth)
        //            {
        //                double valueDepth1 = double.Parse(dtf.Data[i][2].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture);
        //                depthAndKp.TryAdd(valueDepth1, KP);
        //                savePos = j--;
        //                break;
        //            }
        //        }
        //        if (KP != -999)
        //        {
        //            continue;
        //        }
        //        double valueDepth = double.Parse(dtf.Data[i][2].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture);
        //        depthAndKp.TryAdd(valueDepth, KP);
        //    }
        //    return depthAndKp;
        //}
        int a2 = 0;
        // очень долгий метод сохранения данных в Excel
        //public void ExportToExcel(string excelFilePath = null)
        //{
        //    try
        //    {
        //        if (DataTable == null || DataTable.Columns.Count == 0)
        //            throw new Exception("ExportToExcel: Null or empty input table!\n");

        //        // load excel, and create a new workbook
        //        var excelApp = new Microsoft.Office.Interop.Excel.Application();
        //        excelApp.Workbooks.Add();

        //        // single worksheet
        //        Microsoft.Office.Interop.Excel._Worksheet workSheet = (_Worksheet)excelApp.ActiveSheet;

        //        // column headings
        //        for (var i = 0; i < DataTable.Columns.Count; i++)
        //        {
        //            workSheet.Cells[1, i + 1] = DataTable.Columns[i].ColumnName.ToString();
        //        }

        //        // rows
        //        for (var i = 0; i < 102; i++)//dataTable.Rows.Count; i++)
        //        {
        //            // to do: format datetime values before printing
        //            for (var j = 0; j < dataTable.Columns.Count; j++)
        //            {
        //                workSheet.Cells[i + 2, j + 1] = dataTable.Rows[i][j].ToString();
        //            }
        //        }

        //        // check file path
        //        if (!string.IsNullOrEmpty(excelFilePath))
        //        {
        //            try
        //            {
        //                workSheet.SaveAs(excelFilePath);
        //                excelApp.Quit();
        //                MessageBox.Show("Excel file saved!");
        //            }
        //            catch (Exception ex)
        //            {
        //                throw new Exception("ExportToExcel: Excel file could not be saved! Check filepath.\n"
        //                                    + ex.Message);
        //            }
        //        }
        //        else
        //        { // no file path is given
        //            excelApp.Visible = true;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception("ExportToExcel: \n" + ex.Message);
        //    }
        //}
        // работает быстро и оптимально (пока)
       
        int a1 = 0;

        // код поиска глубины сравнением дат
        //// ищем номер столбца с датой
        //int numDate = -1;
        //if (listTimeDepth.Count > 1)
        //{                
        //    for (int i = 0; i < datagrid1.Columns.Count; i++)
        //    {
        //        if (datagrid1.Columns[i].Header.ToString() == "[Время/Дата]")
        //        {
        //            numDate = i;
        //        }
        //    }
        //}

        //if (numDate != -1)
        //{
        //    TimeSpan timeSpan = TimeSpan.FromSeconds(2);                
        //    double[] DepthArray = new double[datagrid1.Items.Count];
        //    int listPos = 0;
        //    // надо 40к изменить потом
        //    for (int k = 40000; k < datagrid1.Items.Count; k ++)
        //    {
        //        DataRowView row = (DataRowView)datagrid1.Items[k];
        //        string text = row.Row.ItemArray[numDate].ToString();
        //        if (DateTime.TryParse(text, out DateTime time1))
        //        {
        //            // проверка вхождения в диапазон
        //            if (time1 >= listTimeDepth[0].Item1 && time1 <= listTimeDepth[listTimeDepth.Count - 1].Item1)
        //            {
        //                for (int i = listPos; i < listTimeDepth.Count; i++)
        //                {
        //                    var time2 = listTimeDepth[i].Item1;
        //                    if (time1 > time2 - timeSpan && time1 <= time2 + timeSpan)
        //                    {
        //                        listPos = i + 1;
        //                        DepthArray[k] = listTimeDepth[i].Item2;
        //                        break;
        //                    }                               
        //                }
        //            }                        
        //        }
        //    }

        //    Depth = DepthArray.ToList();
        //    DataGridTextColumn depthColumn = new DataGridTextColumn();
        //    depthColumn.Header = "Глубина";
        //    depthColumn.Binding = new Binding($"Depth");
        //    datagrid1.Columns.Add(depthColumn);               


        //}
        //else
        //{
        //    ScrollStatusLasTextBox($"Не удалось обнаружить столбец [Время/Дата]");
        //}

        int a = 0;

        // Код кнопки открыть флеш

        //public void MenuItemOpenFile_Click(object sender, RoutedEventArgs e)
        //{
        //    byte[]? FlashFile;
        //    Packet mainPacket = new Packet();
        //    OpenFileDialog openFileDialog = new OpenFileDialog();
        //    openFileDialog.Filter = "Flash Files|*.fl";
        //    openFileDialog.Title = "Выберите flash-файл с данными";
        //    List<List<string>> dataConfig = new List<List<string>>();
        //    string nameFile;

        //    // Получение данных с конфигурационных файлов
        //    List<ConfFileInfo> confFilesInfo = GetConfigData();

        //    if (openFileDialog.ShowDialog() == true)
        //    {
        //        string pathFlash = openFileDialog.FileName;
        //        nameFile = openFileDialog.SafeFileName;
        //        try
        //        {
        //            // считываем данные флеш-файла
        //            FlashFile = File.ReadAllBytes(pathFlash);
        //            FlashFile = FlashFile.Skip(384).ToArray();
        //            string secachPathCong = "";
        //            for (int i = 0; i < FlashFile.Length; i++)
        //            {
        //                foreach (var cf in confFilesInfo)
        //                {
        //                    if (i + cf.LengthLine + 1 < FlashFile.Length)
        //                    {
        //                        bool isStart = cf.StartBytes[0] == FlashFile[i] && cf.StartBytes[1] == FlashFile[i + 1];
        //                        bool isEnd = false;
        //                        // если нет конца строки
        //                        if (cf.EndBytes[0] == 0 && cf.EndBytes[1] == 0)
        //                        {
        //                            isEnd = cf.StartBytes[0] == FlashFile[i + cf.LengthLine] && cf.StartBytes[1] == FlashFile[i + cf.LengthLine + 1];
        //                        }
        //                        else
        //                        {
        //                            isEnd = cf.EndBytes[0] == FlashFile[i + cf.LengthLine - 2] && cf.EndBytes[1] == FlashFile[i + cf.LengthLine - 1];
        //                        }

        //                        if (isStart && isEnd)
        //                        {
        //                            secachPathCong = cf.PathFile;
        //                            mainPacket.ID_Packet = cf.StartBytes[0];
        //                            mainPacket.ID_Device = cf.StartBytes[1];
        //                            mainPacket.endLine[0] = cf.EndBytes[0];
        //                            mainPacket.endLine[1] = cf.EndBytes[1];
        //                            break;
        //                        }
        //                    }
        //                }
        //                if (mainPacket.ID_Device != 0)
        //                {
        //                    break;
        //                }
        //            }

        //            if (mainPacket.ID_Device == 0)
        //            {
        //                ScrollStatusLasTextBox("Возникла ошибка: подходящий конфигурационный файл не найден");
        //                return;
        //            }

        //            if (!string.IsNullOrEmpty(secachPathCong))
        //            {
        //                //считываем данные конфиг - файла

        //                char[] separators = { ' ', '\t' };
        //                using (var reader = new StreamReader(secachPathCong))
        //                {
        //                    bool isStartData = false;
        //                    while (!reader.EndOfStream)
        //                    {
        //                        var row = reader.ReadLine();

        //                        if (!string.IsNullOrWhiteSpace(row))
        //                        {
        //                            if (!isStartData && row.Contains($"~{mainPacket.ID_Packet}"))
        //                            {
        //                                isStartData = true;
        //                            }
        //                            else if (isStartData)
        //                            {
        //                                if (row.StartsWith('#'))
        //                                {
        //                                    break;
        //                                }
        //                                string[] line = row.TrimStart('*').Split(separators, StringSplitOptions.RemoveEmptyEntries);
        //                                dataConfig.Add(new List<string>(line));
        //                            }
        //                        }
        //                    }
        //                }
        //            }

        //        }
        //        catch (Exception ex)
        //        {
        //            ScrollStatusLasTextBox($"Возникла ошибка: {ex.Message}");
        //            return;
        //        }
        //    }
        //    else
        //    {
        //        return;
        //    }

        //    HandleConfigData(dataConfig, mainPacket);
        //    ScrollStatusLasTextBox($"Загрузка файла началась: {nameFile}");
        //    Percent = 0;
        //    UpdateTable(FlashFile, mainPacket);
        //    //FlashFile = null;
        //    //dataConfig = null;            
        //}
    }
}
