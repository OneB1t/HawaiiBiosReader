using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
using System.Collections.ObjectModel;
using System.Globalization;

namespace HawaiBiosReader
{
    public class GridRow
    {
        private String _position;
        private int _value;
        private String _unit;
        private String _type;
        private int _dpm;

        public GridRow(String pos, int val, String un,String type,int dpm)
        {
            _position = pos;
            _value = val;
            _unit = un;
            _type = type;
            _dpm = dpm;
        }
        public int dpm
        {
            get { return _dpm; }
            set { _dpm = value; }
        }
        public String position
        {
            get { return _position; }
            set { _position = value; }
        }


        public int value
        {
            get { return _value; }
            set { _value = value; }
        }

        public String unit
        {
            get { return _unit; }
            set { _unit = value; }
        }
        public String type
        {
            get { return _type; }
            set { _type = value; }
        }

    }

    public partial class MainWindow : Window
    {
        public ObservableCollection<GridRow> data = new ObservableCollection<GridRow>();
        ObservableCollection<GridRow> voltageList = new ObservableCollection<GridRow>();
        ObservableCollection<GridRow> gpuFrequencyList = new ObservableCollection<GridRow>();
        ObservableCollection<GridRow> memFrequencyList = new ObservableCollection<GridRow>();
        ObservableCollection<GridRow> gpumemFrequencyListAndPowerLimit = new ObservableCollection<GridRow>();
        Byte[] romStorageBuffer; // whole rom
        Byte[] powerTablepattern = new Byte[] { 0x02, 0x06, 0x01, 0x00 };
        Byte[] voltageObjectInfoPattern = new Byte[] { 0x08, 0x96, 0x60, 0x00 };
        Byte[] FanControlpattern = new byte[] { 0x07, 0x06, 0x7C, 0x15 }; // pattern to search for in buffer
        Byte[] FanControl2pattern = new byte[] { 0x03, 0x06, 0x7C, 0x15 }; // pattern to search for in buffer
        Byte[] FanControl3pattern = new byte[] { 0x07, 0x06, 0x68, 0x10 }; // pattern to search for in buffer

        int powerTablePosition; // start position of powertable in rom
        int voltageInfoPosition;
        int fanTablePosition;
        int powerTableSize;
        int voltageInfoSize;

        int fanTableOffset = 175;
        int biosNameOffset = 0xDC;
        int tdpLimitOffset = 630;
        int tdcLimitOffset = 632;
        int powerDeliveryLimitOffset = 642;

        // table offsets
        int voltageTableOffset = 319; // 290 have different voltagetable offset than 390
        int memoryFrequencyTableOffset = 278;
        int gpuFrequencyTableOffset = 231;
        int VCELimitTableOffset = 396;
        int AMUAndACPLimitTableOffset = 549;
        int UVDLimitTableOffset = 441;
        string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();


        public MainWindow()
        {
            InitializeComponent();
            versionbox.Text += version;
        }

        private void bOpenFileDialog_Click(object sender, RoutedEventArgs e)
        {
            // Create an instance of the open file dialog box.
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // Set filter options and filter index.
            openFileDialog.Filter = "Bios files (.rom)|*.rom|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = false;

            // Call the ShowDialog method to show the dialog box.
            bool? userClickedOK = openFileDialog.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOK == true)
            {
                // Open the selected file to read.
                System.IO.Stream fileStream = openFileDialog.OpenFile();
                filename.Text = openFileDialog.FileName;

                using (BinaryReader br = new BinaryReader(fileStream)) // binary reader
                {
                    romStorageBuffer = br.ReadBytes((int)fileStream.Length);
                    powerTablePosition = PTPatternAt(romStorageBuffer, powerTablepattern);
                    voltageInfoPosition = PatternAt(romStorageBuffer, voltageObjectInfoPattern);
                    

                    biosName.Text = getTextFromBinary(romStorageBuffer, biosNameOffset, 32);
                    gpuID.Text = romStorageBuffer[565].ToString("X2") + romStorageBuffer[564].ToString("X2") + "-" + romStorageBuffer[567].ToString("X2") + romStorageBuffer[566].ToString("X2"); // not finished working only for few bioses :(

                    if (powerTablePosition == -1)
                    {
                        MessageBoxResult result = MessageBox.Show("PowerTable position not found in this file", "Error", MessageBoxButton.OK);
                    }
                    else
                    {
                        powerTableSize = 256 * romStorageBuffer[powerTablePosition + 1] + romStorageBuffer[powerTablePosition];
                        powerTablesize.Text = powerTableSize.ToString();


                        /*#################################################################################################
                         * 
                         *               BIOS PARSING SECTION
                         * 
                        #################################################################################################*/
                        switch (powerTableSize)
                        {
                            case 660:
                                powerTablesize.Text += " - R9 390/390X";
                                voltageTableOffset = 319;
                                memoryFrequencyTableOffset = 278;
                                gpuFrequencyTableOffset = 231;
                                VCELimitTableOffset = 521;
                                AMUAndACPLimitTableOffset = 549;
                                UVDLimitTableOffset = 439;
                                tdpLimitOffset = 632;
                                tdcLimitOffset = 634;
                                powerDeliveryLimitOffset = 644;
                                break;
                            case 661:
                                powerTablesize.Text += " - R9 295X";
                                voltageTableOffset = 320;
                                memoryFrequencyTableOffset = 279;
                                gpuFrequencyTableOffset = 232;
                                VCELimitTableOffset = 522;
                                AMUAndACPLimitTableOffset = 550;
                                UVDLimitTableOffset = 440;
                                tdpLimitOffset = 633;
                                tdcLimitOffset = 635;
                                powerDeliveryLimitOffset = 645;
                                fanTableOffset = 14632 + 6;
                                break;
                            case 662:
                                powerTablesize.Text += " - R9 390/390X(Sapphire)";
                                voltageTableOffset = 321;
                                memoryFrequencyTableOffset = 280;
                                gpuFrequencyTableOffset = 233;
                                VCELimitTableOffset = 523;
                                AMUAndACPLimitTableOffset = 551;
                                UVDLimitTableOffset = 441;
                                tdpLimitOffset = 634;
                                tdcLimitOffset = 636;
                                powerDeliveryLimitOffset = 646;
                                break;
                            case 650:
                                powerTablesize.Text += " - R9 290X MSI Lightning";
                                voltageTableOffset = 309;
                                memoryFrequencyTableOffset = 268;
                                gpuFrequencyTableOffset = 221;
                                VCELimitTableOffset = 511;
                                AMUAndACPLimitTableOffset = 539;
                                UVDLimitTableOffset = 429;
                                tdpLimitOffset = 622;
                                tdcLimitOffset = 624;
                                powerDeliveryLimitOffset = 634;
                                break;
                            case 648:
                                powerTablesize.Text += " - R9 290/290X";
                                voltageTableOffset = 307;
                                memoryFrequencyTableOffset = 266;
                                gpuFrequencyTableOffset = 219;
                                VCELimitTableOffset = 509;
                                AMUAndACPLimitTableOffset = 537;
                                UVDLimitTableOffset = 427;
                                tdpLimitOffset = 620;
                                tdcLimitOffset = 622;
                                powerDeliveryLimitOffset = 632;
                                break;
                            case 658: // Slith mining bios for 290/290X
                                powerTablesize.Text += " - R9 290/290X The Stilt mining bios";
                                voltageTableOffset = 316;
                                memoryFrequencyTableOffset = 275;
                                gpuFrequencyTableOffset = 228;
                                VCELimitTableOffset = 519;
                                AMUAndACPLimitTableOffset = 547;
                                UVDLimitTableOffset = 437;
                                tdpLimitOffset = 630;
                                tdcLimitOffset = 632;
                                powerDeliveryLimitOffset = 642;
                                break;
                            case 642: // PT1/PT3
                                powerTablesize.Text += " - PT1/PT3 bios";
                                voltageTableOffset = 300;
                                memoryFrequencyTableOffset = 259;
                                gpuFrequencyTableOffset = 212;
                                VCELimitTableOffset = 503;
                                AMUAndACPLimitTableOffset = 531;
                                UVDLimitTableOffset = 421;
                                tdpLimitOffset = 614;
                                tdcLimitOffset = 616;
                                powerDeliveryLimitOffset = 626;
                                break;
                            case 634: // FirePro W9100
                                powerTablesize.Text += " - FirePro W9100";
                                voltageTableOffset = 317;
                                memoryFrequencyTableOffset = 276;
                                gpuFrequencyTableOffset = 229;
                                VCELimitTableOffset = 495;
                                AMUAndACPLimitTableOffset = 523;
                                UVDLimitTableOffset = 425;
                                tdpLimitOffset = 606;
                                tdcLimitOffset = 608;
                                powerDeliveryLimitOffset = 618;
                                break;
                            default:
                                powerTablesize.Text = powerTablesize.Text + " - Unknown type";
                                break;

                        }
                        fanTablePosition = powerTablePosition + fanTableOffset;
                        powerTablePositionValue.Text = "0x" + powerTablePosition.ToString("X");
                        powerTable.Text = getTextFromBinary(romStorageBuffer, powerTablePosition, powerTableSize);

                        int position = 0;
                        gpumemFrequencyListAndPowerLimit.Clear();
                        gpumemFrequencyListAndPowerLimit.Add(new GridRow("0x" + (powerTablePosition + 98).ToString("X"), get24BitValueFromPosition(powerTablePosition + 98, romStorageBuffer, true), "Mhz","24-bit",-1));
                        gpumemFrequencyListAndPowerLimit.Add(new GridRow("0x" + (powerTablePosition + 107).ToString("X"), get24BitValueFromPosition(powerTablePosition + 107, romStorageBuffer, true), "Mhz","24-bit",-1));
                        gpumemFrequencyListAndPowerLimit.Add(new GridRow("0x" + (powerTablePosition + 116).ToString("X"), get24BitValueFromPosition(powerTablePosition + 116, romStorageBuffer, true), "Mhz","24-bit",-1));
                        gpumemFrequencyListAndPowerLimit.Add(new GridRow("0x" + (powerTablePosition + 101).ToString("X"), get24BitValueFromPosition(powerTablePosition + 101, romStorageBuffer, true), "Mhz","24-bit",-1));
                        gpumemFrequencyListAndPowerLimit.Add(new GridRow("0x" + (powerTablePosition + 110).ToString("X"), get24BitValueFromPosition(powerTablePosition + 110, romStorageBuffer, true), "Mhz","24-bit",-1));
                        gpumemFrequencyListAndPowerLimit.Add(new GridRow("0x" + (powerTablePosition + 119).ToString("X"), get24BitValueFromPosition(powerTablePosition + 119, romStorageBuffer, true), "Mhz","24-bit",-1));
                        gpumemFrequencyListAndPowerLimit.Add(new GridRow("0x" + (powerTablePosition + tdpLimitOffset).ToString("X"), get16BitValueFromPosition(powerTablePosition + tdpLimitOffset, romStorageBuffer), "W", "16-bit",-1));
                        gpumemFrequencyListAndPowerLimit.Add(new GridRow("0x" + (powerTablePosition + powerDeliveryLimitOffset).ToString("X"), get16BitValueFromPosition(powerTablePosition + powerDeliveryLimitOffset, romStorageBuffer), "W", "16-bit",-1));
                        gpumemFrequencyListAndPowerLimit.Add(new GridRow("0x" + (powerTablePosition + tdcLimitOffset).ToString("X"), get16BitValueFromPosition(powerTablePosition + tdcLimitOffset, romStorageBuffer), "A", "16-bit",-1));
                        memgpuFrequencyTable.ItemsSource = gpumemFrequencyListAndPowerLimit;


                        // read voltage table
                        voltageList.Clear();
                        for (int i = 0; i < 24; i++)
                        {
                            readValueFromPositionToList(voltageList, (powerTablePosition + voltageTableOffset + (i * 2)), 0, "mV",false,i/3);
                        }
                        voltageEdit.ItemsSource = voltageList;

                        // memory frequency table
                        memFrequencyList.Clear();
                        for (int i = 0; i < 8; i++)
                        {
                            readValueFromPositionToList(memFrequencyList, (powerTablePosition + memoryFrequencyTableOffset + (i * 5)), 1, "Mhz", true,i);
                        }
                        memFrequencyTable.ItemsSource = memFrequencyList;
                        
                        // gpu frequency table
                        gpuFrequencyList.Clear();
                        for (int i = 0; i < 8; i++)
                        {
                            readValueFromPositionToList(gpuFrequencyList, (powerTablePosition + gpuFrequencyTableOffset + (i * 5)), 1, "Mhz", true,i);
                        }
                        gpuFrequencyTable.ItemsSource = gpuFrequencyList;

                        // search for more 24 bit
                        limitValues.Text = "";
                        for (int i = 0; i < 10; i++)
                        {
                            readValueFromPosition(limitValues, powerTablePosition + AMUAndACPLimitTableOffset + 81 + (i * 3), 1, "" + System.Environment.NewLine, false, true);
                        }

                        // search for more 16 bit
                        limitValues2.Text = "";
                        for (int i = 0; i < 16; i++)
                        {

                            readValueFromPosition(limitValues2, powerTablePosition + AMUAndACPLimitTableOffset + 79 + (i * 2), 0, "" + System.Environment.NewLine, false, true);
                        }
                        // search for more 24 bit

                        if (voltageInfoPosition > 0)
                        {
                            voltageinfo.Text = "";
                            for (int i = 0; i < 20; i++)
                            {
                                readValueFromPosition(voltageinfo, powerTablePosition + voltageInfoPosition + 1 + (i * 3), 1, "" + System.Environment.NewLine, false, true);
                            }

                            // search for more 16 bit
                            voltageinfo2.Text = "";
                            for (int i = 0; i < 32; i++)
                            {
                                readValueFromPosition(voltageinfo2, powerTablePosition + voltageInfoPosition + 1 + (i * 2), 0, "" + System.Environment.NewLine, false, true);
                            }
                        }
                        // StartVCELimitTable
                        VCELimitTableValues.Text = "";
                        for (int i = 0; i < 8; i++)
                        {
                            position = powerTablePosition + VCELimitTableOffset + (i * 3);
                            readValueFromPosition(VCELimitTableValues, position, 0, "--", false, true);
                            VCELimitTableValues.Text += romStorageBuffer[position + 2] + System.Environment.NewLine;
                        }

                        // StartUVDLimitTable
                        UVDLimitTable.Text = "";
                        for (int i = 0; i < 8; i++)
                        {
                            position = powerTablePosition + UVDLimitTableOffset + (i * 3);
                            readValueFromPosition(UVDLimitTable, position, 0, "--", false, true);
                            UVDLimitTable.Text += romStorageBuffer[position + 2] + System.Environment.NewLine;
                        }

                        // StartSAMULimitTable + StartACPLimitTable
                        AMULimitTable.Text = "";
                        ACPLimitTable.Text = "";
                        for (int i = 0; i < 16; i++)
                        {
                            if (i <= 7)
                            {
                                position = powerTablePosition + AMUAndACPLimitTableOffset + (i * 5);
                                AMULimitTable.Text += "0x" + position.ToString("X") + "  -- ";
                                AMULimitTable.Text += get16BitValueFromPosition(position - 2, romStorageBuffer) + "  -- ";
                                AMULimitTable.Text += get24BitValueFromPosition(position, romStorageBuffer) + System.Environment.NewLine;
                            }
                            else
                            {
                                position = powerTablePosition + AMUAndACPLimitTableOffset + 2 + (i * 5);
                                ACPLimitTable.Text += "0x" + position.ToString("X") + "  -- ";
                                ACPLimitTable.Text += get16BitValueFromPosition(position - 2, romStorageBuffer) + "  -- ";
                                ACPLimitTable.Text += get24BitValueFromPosition(position, romStorageBuffer) + System.Environment.NewLine;
                            }
                        }


                        if (fanTablePosition > 0)
                        {
                            readValueFromPosition(temperatureHysteresis, fanTablePosition + 1, 2, "C°");
                            readValueFromPosition(fantemperature1, fanTablePosition + 2, 0, "C°", true);
                            readValueFromPosition(fantemperature2, fanTablePosition + 4, 0, "C°", true);
                            readValueFromPosition(fantemperature3, fanTablePosition + 6, 0, "C°", true);
                            readValueFromPosition(fantemperature4, fanTablePosition + 14, 0, "C°", true);

                            readValueFromPosition(fanspeed1, fanTablePosition + 8, 0, "%", true);
                            readValueFromPosition(fanspeed2, fanTablePosition + 10, 0, "%", true);
                            readValueFromPosition(fanspeed3, fanTablePosition + 12, 0, "%", true);
                            readValueFromPosition(fanControlType, fanTablePosition + 16, 2, "", true);
                            readValueFromPosition(pwmFanMax, fanTablePosition + 17, 2, "%");
                            readValueFromPosition(maxAsicTemperature, fanTablePosition + 459, 2, "C°");
                            // 
                            readValueFromPosition(gpuMaxClock, fanTablePosition + 33, 1, "Mhz");// this offset work only for 390X need some polishing for other cards
                            readValueFromPosition(memMaxClock, fanTablePosition + 37, 1, "Mhz");
                        }
                        else
                        {
                            temperatureHysteresis.Text = "NOT FOUND";
                            fanControlType.Text = "NOT FOUND";
                            pwmFanMax.Text = "NOT FOUND";
                            maxAsicTemperature.Text = "NOT FOUND";
                            fanspeed1.Text = "NOT FOUND";
                            fanspeed2.Text = "NOT FOUND";
                            fanspeed3.Text = "NOT FOUND";
                            fantemperature1.Text = "NOT FOUND";
                            fantemperature2.Text = "NOT FOUND";
                            fantemperature3.Text = "NOT FOUND";
                            fantemperature4.Text = "NOT FOUND";
                        }
                    }
                    fileStream.Close();
                }
            }
        }
        public void readValueFromPosition(TextBox dest, int position, int type, String units = "", bool isFrequency = false, bool add = false)
        {
            if (add)
            {
                dest.Text += "0x" + position.ToString("X") + " -- ";
            }
            else
            {
                dest.Text = "0x" + position.ToString("X") + " -- ";
            }

            switch (type)
            {
                case 0: // 16 bit value
                    dest.Text += get16BitValueFromPosition(position, romStorageBuffer, isFrequency).ToString() + " " + units;
                    break;
                case 1: // 24 bit value
                    dest.Text += get24BitValueFromPosition(position, romStorageBuffer, isFrequency).ToString() + " " + units;
                    break;
                case 2: // 8 bit value
                    dest.Text += romStorageBuffer[position].ToString() + " " + units;
                    break;
                default:
                    dest.Text += get16BitValueFromPosition(position, romStorageBuffer, isFrequency).ToString() + " " + units;
                    break;
            }
        }
        public void readValueFromPositionToList(ObservableCollection<GridRow> dest, int position, int type, String units = "", bool isFrequency = false,int dpm = -1)
        {

            switch (type)
            {
                case 0: // 16 bit value
                    dest.Add(new GridRow("0x" + position.ToString("X"), get16BitValueFromPosition(position, romStorageBuffer, isFrequency), units,"16-bit",dpm));
                    break;
                case 1: // 24 bit value
                    dest.Add(new GridRow("0x" + position.ToString("X"), get24BitValueFromPosition(position, romStorageBuffer, isFrequency), units,"24-bit",dpm));
                    break;
                case 2: // 8 bit value
                    dest.Add(new GridRow("0x" + position.ToString("X"), get8BitValueFromPosition(position, romStorageBuffer, isFrequency), units,"8-bit",dpm));
                    break;
                default:
                    dest.Add(new GridRow("0x" + position.ToString("X"), get8BitValueFromPosition(position, romStorageBuffer, isFrequency), units,"8-bit",dpm));
                    break;
            }
        }
        private static int PTPatternAt(byte[] data, byte[] pattern)
        {
            for (int di = 0; di < data.Length; di++)
                if (data[di] == pattern[0] && data[di + 1] == pattern[1] && data[di + 2] == pattern[2] && data[di + 3] == pattern[3])
                {
                    return di - 1;
                }
            return -1;
        }

        private static int PatternAt(byte[] data, byte[] pattern)
        {
            for (int i = 0; i < data.Length; )
            {
                int j;
                for (j = 0; j < pattern.Length; j++)
                {
                    if (pattern[j] != data[i])
                        break;
                    i++;
                }
                if (j == pattern.Length)
                {
                    return i - pattern.Length;
                }
                if (j != 0) continue;
                i++;
            }
            return -1;
        }
        public String getTextFromBinary(byte[] binary, int offset, int lenght)
        {
            System.Text.Encoding encEncoder = System.Text.ASCIIEncoding.ASCII;
            string str = encEncoder.GetString(binary.Skip(offset).Take(lenght).ToArray());
            return str;
        }
        // dumb way to extract 24 bit value (can be made much more effective but this is easy to read for anyone)
        public Int32 get24BitValueFromPosition(int position, byte[] buffer, bool isFrequency = false)
        {
            if (position < buffer.Length - 2)
            {
                if (isFrequency) // if its frequency divide by 100 to convert it into Mhz
                {
                    return (256 * 256 * buffer[position + 2] + 256 * buffer[position + 1] + buffer[position]) / 100;
                }
                return 256 * 256 * buffer[position + 2] + 256 * buffer[position + 1] + buffer[position];
            }
            return -1;
        }

        public Int32 get16BitValueFromPosition(int position, byte[] buffer, bool isFrequency = false)
        {
            if (position < buffer.Length - 1)
            {
                if (isFrequency) // if its frequency divide by 100 to convert it into Mhz
                {
                    return (256 * buffer[position + 1] + buffer[position]) / 100;
                }
                return 256 * buffer[position + 1] + buffer[position];
            }
            return -1;
        }
        public Int32 get8BitValueFromPosition(int position, byte[] buffer, bool isFrequency = false)
        {
            if (position < buffer.Length)
            {
                if (isFrequency) // if its frequency divide by 100 to convert it into Mhz
                {
                    return buffer[position] / 100;
                }
                return buffer[position];
            }
            return -1;
        }


        private void bSaveFileDialog_Click(object sender, RoutedEventArgs e)
        {
           SaveFileDialog SaveFileDialog = new SaveFileDialog();
            SaveFileDialog.Title = "Save As...";
            SaveFileDialog.Filter = "Bios File (*.rom)|*.rom";
             bool? userClickedOK = SaveFileDialog.ShowDialog();
            if (userClickedOK == true)
            {
                FileStream fs = new FileStream(SaveFileDialog.FileName, FileMode.Create);
                // Create the writer for data.
                BinaryWriter bw = new BinaryWriter(fs);

                // save our changes
                saveList(voltageList,true); // there are values which are not frequency but it works as they are only singlevalue
                saveList(memFrequencyList,true);
                saveList(gpuFrequencyList,true);
                saveList(gpumemFrequencyListAndPowerLimit, true);


                bw.Write(romStorageBuffer);

                fs.Close();
                bw.Close();
            }
         }

        private void saveList(ObservableCollection<GridRow> list, bool isFrequency = false)
        {
            foreach (GridRow row in list)
            {
                byte[] bytes;
                int x;
                int value = row.value;
                if (row.position.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
                {
                    row.position = row.position.Substring(2);
                }
                if (isFrequency)
                {
                    value *= 100;
                }
                if (int.TryParse(row.position, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out x))
                {
                    switch (row.type)
                    {
                        case "24-bit":
                            {
                                bytes = new byte[3];
                                bytes[0] = (byte)value;
                                bytes[1] = (byte)(value >> 8);
                                bytes[2] = (byte)(value >> 16);
                                // this is for 24 bit
                                romStorageBuffer[x] = bytes[0];
                                romStorageBuffer[x + 1] = bytes[1];
                                romStorageBuffer[x + 2] = bytes[2];
                                break;
                            }
                        case "16-bit":
                            {
                                bytes = new byte[2];
                                bytes[0] = (byte)row.value;
                                bytes[1] = (byte)(row.value >> 8);
                                romStorageBuffer[x] = bytes[0];
                                romStorageBuffer[x + 1] = bytes[1];
                                break;
                            }
                        case "8-bit":
                            {
                                bytes = new byte[1];
                                bytes[0] = (byte)row.value;
                                romStorageBuffer[x] = bytes[0];
                                break;
                            }
                    }
                }
            }
        }

        private void voltageEdit_GotFocus(object sender, RoutedEventArgs e)
        {
            voltageEdit.Columns[0].IsReadOnly = true;
            voltageEdit.Columns[1].IsReadOnly = false;
            voltageEdit.Columns[2].IsReadOnly = true;
            voltageEdit.Columns[3].IsReadOnly = true;
            voltageEdit.Columns[4].IsReadOnly = true;

        }
        private void gpuFrequencyTable_GotFocus(object sender, RoutedEventArgs e)
        {
            gpuFrequencyTable.Columns[0].IsReadOnly = true;
            gpuFrequencyTable.Columns[1].IsReadOnly = false;
            gpuFrequencyTable.Columns[2].IsReadOnly = true;
            gpuFrequencyTable.Columns[3].IsReadOnly = true;
            gpuFrequencyTable.Columns[4].IsReadOnly = true;

        }

        private void memFrequencyTable_GotFocus(object sender, RoutedEventArgs e)
        {
            memFrequencyTable.Columns[0].IsReadOnly = true;
            memFrequencyTable.Columns[1].IsReadOnly = false;
            memFrequencyTable.Columns[2].IsReadOnly = true;
            memFrequencyTable.Columns[3].IsReadOnly = true;
            memFrequencyTable.Columns[4].IsReadOnly = true;
        }

        private void memgpuFrequencyTable_GotFocus(object sender, RoutedEventArgs e)
        {
            memgpuFrequencyTable.Columns[0].IsReadOnly = true;
            memgpuFrequencyTable.Columns[1].IsReadOnly = false;
            memgpuFrequencyTable.Columns[2].IsReadOnly = true;
            memgpuFrequencyTable.Columns[3].IsReadOnly = true;
            memgpuFrequencyTable.Columns[4].IsReadOnly = true;
        }

        }
    }
