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
using HawaiiBiosReader;

namespace HawaiBiosReader
{

    public partial class MainWindow : Window
    {
        ObservableCollection<GridRow> data = new ObservableCollection<GridRow>();
        ObservableCollection<GridRowVoltage> voltageList = new ObservableCollection<GridRowVoltage>();
        ObservableCollection<GridRowVoltage> gpumemFrequencyListAndPowerLimit = new ObservableCollection<GridRowVoltage>();
        ObservableCollection<GridRowVoltage> fanList = new ObservableCollection<GridRowVoltage>();
        ObservableCollection<GridRowVoltage> memoryTimingList = new ObservableCollection<GridRowVoltage>();

        ObservableCollection<GridRow> gpuFrequencyList = new ObservableCollection<GridRow>();
        ObservableCollection<GridRow> memFrequencyList = new ObservableCollection<GridRow>();
        ObservableCollection<GridRow> VCELimitTableData = new ObservableCollection<GridRow>();
        ObservableCollection<GridRow> UVDLimitTableData = new ObservableCollection<GridRow>();
        ObservableCollection<GridRow> SAMULimitTableData = new ObservableCollection<GridRow>();
        ObservableCollection<GridRow> ACPLimitTableData = new ObservableCollection<GridRow>();

        Byte[] romStorageBuffer; // whole rom
        Byte[] powerTablepattern = new Byte[] { 0x02, 0x06, 0x01, 0x00 };
        Byte[] voltageObjectInfoPattern = new Byte[] { 0x00, 0x03, 0x01, 0x01, 0x03 };
        Byte[] memoryTimingPattern = new Byte[] { 0xDE, 0x09, 0x84, 0xFF, 0xFF, 0x00 }; // thanks Lard

        // unknown table offsets
        int powerTablePosition;
        int voltageInfoPosition;
        int fanTablePosition;
        int powerTableSize;
        int developTablePosition;
        int memoryTimingsPosition;

        // table offsets for default
        int fanTableOffset = 175;
        int biosNameOffset = 220;
        int tdpLimitOffset = 630;
        int tdcLimitOffset = 632;
        int powerDeliveryLimitOffset = 642;

        int memoryFrequencyTableOffset = 278;
        int gpuFrequencyTableOffset = 231;
        int VCELimitTableOffset = 396;
        int AMUAndACPLimitTableOffset = 547;
        int UVDLimitTableOffset = 441;
        string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); // program version


        public MainWindow()
        {
            InitializeComponent();
            versionbox.Text += version;
            MainWindow.GetWindow(this).Title += " " + version;
        }

        private void OpenFileDialog_Click(object sender, RoutedEventArgs e)
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
                    fixChecksum(false);
                    powerTablePosition = PTPatternAt(romStorageBuffer, powerTablepattern);
                    memoryTimingsPosition = PTPatternAt(romStorageBuffer, memoryTimingPattern);
                    voltageInfoPosition = PatternAt(romStorageBuffer, voltageObjectInfoPattern) - 1;


                    biosName.Text = getTextFromBinary(romStorageBuffer, biosNameOffset, 32);
                    gpuID.Text = romStorageBuffer[565].ToString("X2") + romStorageBuffer[564].ToString("X2") + "-" + romStorageBuffer[567].ToString("X2") + romStorageBuffer[566].ToString("X2"); // not finished working only for few bioses :(

                    if (powerTablePosition == -1)
                    {
                        MessageBoxResult result = MessageBox.Show("PowerTable search position not found in this file", "Error", MessageBoxButton.OK);
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
                            case 520: // FirePro W8100
                                powerTablesize.Text += " - FirePro W8100 - NOT FULLY SUPPORTED";
                                memoryFrequencyTableOffset = 261;
                                gpuFrequencyTableOffset = 229;
                                break;
                            case 660:
                                powerTablesize.Text += " - R9 390/390X";
                                memoryFrequencyTableOffset = 278;
                                gpuFrequencyTableOffset = 231;
                                VCELimitTableOffset = 521;
                                AMUAndACPLimitTableOffset = 547;
                                UVDLimitTableOffset = 439;
                                tdpLimitOffset = 632;
                                tdcLimitOffset = 634;
                                powerDeliveryLimitOffset = 644;
                                break;
                            case 661:
                                powerTablesize.Text += " - R9 295X";
                                memoryFrequencyTableOffset = 279;
                                gpuFrequencyTableOffset = 232;
                                VCELimitTableOffset = 522;
                                AMUAndACPLimitTableOffset = 548;
                                UVDLimitTableOffset = 440;
                                tdpLimitOffset = 633;
                                tdcLimitOffset = 635;
                                powerDeliveryLimitOffset = 645;
                                fanTableOffset = 14632 + 6;
                                break;
                            case 662:
                                powerTablesize.Text += " - R9 390/390X (Sapphire)";
                                memoryFrequencyTableOffset = 280;
                                gpuFrequencyTableOffset = 233;
                                VCELimitTableOffset = 523;
                                AMUAndACPLimitTableOffset = 549;
                                UVDLimitTableOffset = 441;
                                tdpLimitOffset = 634;
                                tdcLimitOffset = 636;
                                powerDeliveryLimitOffset = 646;
                                break;
                            case 669:// XFX R9 290X Double Dissipation
                                powerTablesize.Text += " - XFX R9 290X Double Dissipation";
                                memoryFrequencyTableOffset = 287;
                                gpuFrequencyTableOffset = 240;
                                VCELimitTableOffset = 530;
                                AMUAndACPLimitTableOffset = 556;
                                UVDLimitTableOffset = 448;
                                tdpLimitOffset = 641;
                                tdcLimitOffset = 643;
                                powerDeliveryLimitOffset = 653;
                                break;
                            case 671:// The Slith edited roms
                                powerTablesize.Text += " - R9 290X The Slith roms";
                                memoryFrequencyTableOffset = 289;
                                gpuFrequencyTableOffset = 242;
                                VCELimitTableOffset = 532;
                                AMUAndACPLimitTableOffset = 558;
                                UVDLimitTableOffset = 450;
                                tdpLimitOffset = 643;
                                tdcLimitOffset = 645;
                                powerDeliveryLimitOffset = 655;
                                fanTableOffset = 184;
                                break;
                            case 650:
                                powerTablesize.Text += " - R9 290X (MSI Lightning)";
                                memoryFrequencyTableOffset = 268;
                                gpuFrequencyTableOffset = 221;
                                VCELimitTableOffset = 511;
                                AMUAndACPLimitTableOffset = 537;
                                UVDLimitTableOffset = 429;
                                tdpLimitOffset = 622;
                                tdcLimitOffset = 624;
                                powerDeliveryLimitOffset = 634;
                                break;
                            case 648:
                                powerTablesize.Text += " - R9 290/290X";
                                memoryFrequencyTableOffset = 266;
                                gpuFrequencyTableOffset = 219;
                                VCELimitTableOffset = 509;
                                AMUAndACPLimitTableOffset = 535;
                                UVDLimitTableOffset = 427;
                                tdpLimitOffset = 620;
                                tdcLimitOffset = 622;
                                powerDeliveryLimitOffset = 632;
                                break;
                            case 658: // The Stilt mining bios for 290/290X
                                powerTablesize.Text += " - R9 290/290X (The Stilt)";
                                memoryFrequencyTableOffset = 275;
                                gpuFrequencyTableOffset = 228;
                                VCELimitTableOffset = 519;
                                AMUAndACPLimitTableOffset = 545;
                                UVDLimitTableOffset = 437;
                                tdpLimitOffset = 630;
                                tdcLimitOffset = 632;
                                powerDeliveryLimitOffset = 642;
                                break;
                            case 642: // PT1/PT3
                                powerTablesize.Text += " - R9 290/290X (PT1/PT3)";
                                memoryFrequencyTableOffset = 259;
                                gpuFrequencyTableOffset = 212;
                                VCELimitTableOffset = 503;
                                AMUAndACPLimitTableOffset = 529;
                                UVDLimitTableOffset = 421;
                                tdpLimitOffset = 614;
                                tdcLimitOffset = 616;
                                powerDeliveryLimitOffset = 626;
                                break;
                            case 634: // FirePro W9100
                                powerTablesize.Text += " - FirePro W9100";
                                memoryFrequencyTableOffset = 276;
                                gpuFrequencyTableOffset = 229;
                                VCELimitTableOffset = 495;
                                AMUAndACPLimitTableOffset = 521;
                                UVDLimitTableOffset = 425;
                                tdpLimitOffset = 606;
                                tdcLimitOffset = 608;
                                powerDeliveryLimitOffset = 618;
                                break;
                            default:
                                powerTablesize.Text += " - Unknown type";
                                break;
                        }

                        fanTablePosition = powerTablePosition + fanTableOffset;
                        powerTablePositionValue.Text = "0x" + powerTablePosition.ToString("X");


                        gpumemFrequencyListAndPowerLimit.Clear();
                        gpumemFrequencyListAndPowerLimit.Add(new GridRowVoltage("0x" + (powerTablePosition + 98).ToString("X"), get24BitValueFromPosition(powerTablePosition + 98, romStorageBuffer, true), "Mhz", "24-bit"));
                        gpumemFrequencyListAndPowerLimit.Add(new GridRowVoltage("0x" + (powerTablePosition + 107).ToString("X"), get24BitValueFromPosition(powerTablePosition + 107, romStorageBuffer, true), "Mhz", "24-bit"));
                        gpumemFrequencyListAndPowerLimit.Add(new GridRowVoltage("0x" + (powerTablePosition + 116).ToString("X"), get24BitValueFromPosition(powerTablePosition + 116, romStorageBuffer, true), "Mhz", "24-bit"));
                        gpumemFrequencyListAndPowerLimit.Add(new GridRowVoltage("0x" + (powerTablePosition + 101).ToString("X"), get24BitValueFromPosition(powerTablePosition + 101, romStorageBuffer, true), "Mhz", "24-bit"));
                        gpumemFrequencyListAndPowerLimit.Add(new GridRowVoltage("0x" + (powerTablePosition + 110).ToString("X"), get24BitValueFromPosition(powerTablePosition + 110, romStorageBuffer, true), "Mhz", "24-bit"));
                        gpumemFrequencyListAndPowerLimit.Add(new GridRowVoltage("0x" + (powerTablePosition + 119).ToString("X"), get24BitValueFromPosition(powerTablePosition + 119, romStorageBuffer, true), "Mhz", "24-bit"));
                        gpumemFrequencyListAndPowerLimit.Add(new GridRowVoltage("0x" + (powerTablePosition + tdpLimitOffset).ToString("X"), get16BitValueFromPosition(powerTablePosition + tdpLimitOffset, romStorageBuffer), "W", "16-bit"));
                        gpumemFrequencyListAndPowerLimit.Add(new GridRowVoltage("0x" + (powerTablePosition + powerDeliveryLimitOffset).ToString("X"), get16BitValueFromPosition(powerTablePosition + powerDeliveryLimitOffset, romStorageBuffer), "W", "16-bit"));
                        gpumemFrequencyListAndPowerLimit.Add(new GridRowVoltage("0x" + (powerTablePosition + tdcLimitOffset).ToString("X"), get16BitValueFromPosition(powerTablePosition + tdcLimitOffset, romStorageBuffer), "A", "16-bit"));

                        memgpuFrequencyTable.ItemsSource = gpumemFrequencyListAndPowerLimit;

                        // memory frequency table
                        memFrequencyList.Clear();
                        int gpuFrequencyTableCount = get8BitValueFromPosition(powerTablePosition + memoryFrequencyTableOffset - 1, romStorageBuffer);
                        for (int i = 0; i < gpuFrequencyTableCount; i++)
                        {
                            readValueFromPositionToList(memFrequencyList, (powerTablePosition + memoryFrequencyTableOffset + (i * 5)), 1, "Mhz", true, i);
                        }
                        memFrequencyTable.ItemsSource = memFrequencyList;

                        // gpu frequency table
                        gpuFrequencyList.Clear();
                        int memoryFrequencyTableCount = get8BitValueFromPosition(powerTablePosition + gpuFrequencyTableOffset - 1, romStorageBuffer);
                        for (int i = 0; i < memoryFrequencyTableCount; i++)
                        {
                            readValueFromPositionToList(gpuFrequencyList, (powerTablePosition + gpuFrequencyTableOffset + (i * 5)), 1, "Mhz", true, i);
                        }
                        gpuFrequencyTable.ItemsSource = gpuFrequencyList;

                        int position = 0;
                        // StartVCELimitTable
                        VCELimitTableData.Clear();
                        for (int i = 0; i < 8; i++)
                        {
                            position = powerTablePosition + VCELimitTableOffset + (i * 3);
                            VCELimitTableData.Add(new GridRow("0x" + (position + 2).ToString("X"),  get8BitValueFromPosition(position + 2, romStorageBuffer), "DPM", "8-bit", i, "0x" + (position).ToString("X"),get16BitValueFromPosition(position, romStorageBuffer, false)));
                        }
                        VCELimitTable.ItemsSource = VCELimitTableData;

                        // StartUVDLimitTable
                        UVDLimitTableData.Clear();
                        for (int i = 0; i < 8; i++)
                        {
                            position = powerTablePosition + UVDLimitTableOffset + (i * 3);
                            UVDLimitTableData.Add(new GridRow("0x" + (position + 2).ToString("X"), get8BitValueFromPosition(position + 2, romStorageBuffer), "DPM", "8-bit", i, "0x" + (position).ToString("X"), get16BitValueFromPosition(position, romStorageBuffer, false)));
                        }
                        UVDLimitTable.ItemsSource = UVDLimitTableData;

                        // StartSAMULimitTable + StartACPLimitTable
                        SAMULimitTableData.Clear();
                        for (int i = 0; i < 8; i++)
                        {
                            position = powerTablePosition + AMUAndACPLimitTableOffset + (i * 5);
                            SAMULimitTableData.Add(new GridRow("0x" + (position + 2).ToString("X"), get24BitValueFromPosition(position + 2, romStorageBuffer), "%", "24-bit", i, "0x" + (position).ToString("X"), get16BitValueFromPosition(position, romStorageBuffer, false)));
                        }
                        SAMULimitTable.ItemsSource = SAMULimitTableData;


                        ACPLimitTableData.Clear();
                        for (int i = 0; i < 8; i++)
                        {
                            position = powerTablePosition + AMUAndACPLimitTableOffset + 42 + (i * 5);
                            ACPLimitTableData.Add(new GridRow("0x" + (position + 2).ToString("X"), get24BitValueFromPosition(position + 2, romStorageBuffer), "%", "24-bit", i, "0x" + (position).ToString("X"), get16BitValueFromPosition(position, romStorageBuffer, false)));
                        }
                        ACPLimitTable.ItemsSource = ACPLimitTableData;

                        if(memoryTimingsPosition > 0)
                        {
                            memoryTimingList.Clear();
                            memoryTimingList.Add(new GridRowVoltage("0x" + (memoryTimingsPosition + 1).ToString("X"), get16BitValueFromPosition(memoryTimingsPosition + 1, romStorageBuffer), "ms", "16-bit"));
                            memoryTimingList.Add(new GridRowVoltage("0x" + (memoryTimingsPosition + 3).ToString("X"), get16BitValueFromPosition(memoryTimingsPosition + 3, romStorageBuffer), "ms", "16-bit"));
                            memoryTimingList.Add(new GridRowVoltage("0x" + (memoryTimingsPosition + 5).ToString("X"), get16BitValueFromPosition(memoryTimingsPosition + 5, romStorageBuffer), "ms", "16-bit"));
                            memoryTimingList.Add(new GridRowVoltage("0x" + (memoryTimingsPosition + 7).ToString("X"), get16BitValueFromPosition(memoryTimingsPosition + 7, romStorageBuffer), "ms", "16-bit"));
                            memoryTimingList.Add(new GridRowVoltage("0x" + (memoryTimingsPosition + 9).ToString("X"), get16BitValueFromPosition(memoryTimingsPosition + 9, romStorageBuffer), "ms", "16-bit"));
                            memoryTimingList.Add(new GridRowVoltage("0x" + (memoryTimingsPosition + 11).ToString("X"), get16BitValueFromPosition(memoryTimingsPosition + 11, romStorageBuffer), "ms", "16-bit"));
                            memoryTimingList.Add(new GridRowVoltage("0x" + (memoryTimingsPosition + 13).ToString("X"), get16BitValueFromPosition(memoryTimingsPosition + 13, romStorageBuffer), "ms", "16-bit"));
                            memoryTimingList.Add(new GridRowVoltage("0x" + (memoryTimingsPosition + 15).ToString("X"), get16BitValueFromPosition(memoryTimingsPosition + 15, romStorageBuffer), "ms", "16-bit"));
                            memoryTimingList.Add(new GridRowVoltage("0x" + (memoryTimingsPosition + 17).ToString("X"), get16BitValueFromPosition(memoryTimingsPosition + 17, romStorageBuffer), "ms", "16-bit"));
                            memoryTimingList.Add(new GridRowVoltage("0x" + (memoryTimingsPosition + 19).ToString("X"), get16BitValueFromPosition(memoryTimingsPosition + 19, romStorageBuffer), "ms", "16-bit"));
                            memoryTimingTable.ItemsSource = memoryTimingList;
                        }

                        if (fanTablePosition > 0)
                        {

                            fanList.Clear();
                            fanList.Add(new GridRowVoltage("0x" + (fanTablePosition + 1).ToString("X"), get8BitValueFromPosition(fanTablePosition + 1, romStorageBuffer), "°C", "8-bit")); //temperatureHysteresis
                            fanList.Add(new GridRowVoltage("0x" + (fanTablePosition + 2).ToString("X"), get16BitValueFromPosition(fanTablePosition + 2, romStorageBuffer,true), "°C", "16-bit")); //fantemperature1
                            fanList.Add(new GridRowVoltage("0x" + (fanTablePosition + 4).ToString("X"), get16BitValueFromPosition(fanTablePosition + 4, romStorageBuffer, true), "°C", "16-bit")); //fantemperature2
                            fanList.Add(new GridRowVoltage("0x" + (fanTablePosition + 6).ToString("X"), get16BitValueFromPosition(fanTablePosition + 6, romStorageBuffer, true), "°C", "16-bit")); //fantemperature3
                            fanList.Add(new GridRowVoltage("0x" + (fanTablePosition + 8).ToString("X"), get16BitValueFromPosition(fanTablePosition + 8, romStorageBuffer, true), "°C", "16-bit")); //fanspeed1
                            fanList.Add(new GridRowVoltage("0x" + (fanTablePosition + 10).ToString("X"), get16BitValueFromPosition(fanTablePosition + 10, romStorageBuffer, true), "°C", "16-bit")); //fanspeed2
                            fanList.Add(new GridRowVoltage("0x" + (fanTablePosition + 12).ToString("X"), get16BitValueFromPosition(fanTablePosition + 12, romStorageBuffer, true), "°C", "16-bit")); //fanspeed3
                            fanList.Add(new GridRowVoltage("0x" + (fanTablePosition + 14).ToString("X"), get16BitValueFromPosition(fanTablePosition + 14, romStorageBuffer, true), "°C", "16-bit")); //fantemperature4
                            fanList.Add(new GridRowVoltage("0x" + (fanTablePosition + 16).ToString("X"), get8BitValueFromPosition(fanTablePosition + 16, romStorageBuffer), "1/0", "8-bit")); //fanControlType
                            fanList.Add(new GridRowVoltage("0x" + (fanTablePosition + 17).ToString("X"), get16BitValueFromPosition(fanTablePosition + 17, romStorageBuffer), "°C", "8-bit")); //pwmFanMax
                            fanTable.ItemsSource = fanList;

                            readValueFromPosition(gpuMaxClock, fanTablePosition + 33, 1, "Mhz");  // this offset work only for 390X need some polishing for other cards
                            readValueFromPosition(memMaxClock, fanTablePosition + 37, 1, "Mhz");
                        }
                    }
                    fileStream.Close();
                }
            }
        }


        /*#################################################################################################
         * 
         *               HELPER FUNCTIONS
         * 
        #################################################################################################*/
        public void readValueFromPosition(TextBox dest, int position, int type, String units = "", bool isFrequency = false, bool add = false, bool voltage = false)
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
                    if (voltage)
                        dest.Text += (get16BitValueFromPosition(position, romStorageBuffer, isFrequency) * 6.25).ToString() + " " + units;
                    else
                        dest.Text += get16BitValueFromPosition(position, romStorageBuffer, isFrequency).ToString() + " " + units;
                    break;
                case 1: // 24 bit value
                    dest.Text += get24BitValueFromPosition(position, romStorageBuffer, isFrequency).ToString() + " " + units;
                    break;
                case 2: // 8 bit value
                    if (voltage)
                        dest.Text += (romStorageBuffer[position] * 6.25).ToString() + " " + units;
                    else
                        dest.Text += romStorageBuffer[position].ToString() + " " + units;
                    break;
                case 3: // 32 bit value
                    dest.Text += get32BitValueFromPosition(position, romStorageBuffer, isFrequency).ToString() + " " + units;
                    break;
                default:
                    dest.Text += get16BitValueFromPosition(position, romStorageBuffer, isFrequency).ToString() + " " + units;
                    break;
            }
        }
        public Byte readValueFromPositionDevelop(TextBox dest, int position, int type, String units = "", bool isFrequency = false, bool add = false, bool voltage = false)
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
                    developTablePosition += 2;
                    return 0;
                case 2: // 8 bit value
                    dest.Text += romStorageBuffer[position].ToString() + " " + units;
                    developTablePosition++;
                    return romStorageBuffer[position];
                case 4: // 32 bit value
                    dest.Text += get32BitValueFromPosition(position, romStorageBuffer, isFrequency).ToString() + " " + units;
                    developTablePosition += 4;
                    return 0;
            }
            return 0;
        }

        public void readValueFromPositionToList(ObservableCollection<GridRow> dest, int position, int type, String units = "", bool isFrequency = false, int dpm = -1)
        {
            switch (type)
            {
                case 0: // 16 bit value
                    dest.Add(new GridRow("0x" + position.ToString("X"), get16BitValueFromPosition(position, romStorageBuffer, isFrequency), units, "16-bit", dpm, "0x" + (position + 2).ToString("X"), get16BitValueFromPosition(position + 2, romStorageBuffer)));
                    break;
                case 1: // 24 bit value
                    dest.Add(new GridRow("0x" + position.ToString("X"), get24BitValueFromPosition(position, romStorageBuffer, isFrequency), units, "24-bit", dpm, "0x" + (position + 3).ToString("X"), get16BitValueFromPosition(position + 3, romStorageBuffer)));
                    break;
                case 2: // 8 bit value
                    dest.Add(new GridRow("0x" + position.ToString("X"), get8BitValueFromPosition(position, romStorageBuffer, isFrequency), units, "8-bit", dpm, "0x" + (position + 1).ToString("X"), get16BitValueFromPosition(position + 1, romStorageBuffer)));
                    break;
                default:
                    dest.Add(new GridRow("0x" + position.ToString("X"), get8BitValueFromPosition(position, romStorageBuffer, isFrequency), units, "8-bit", dpm, "0x" + (position + 1).ToString("X"), get16BitValueFromPosition(position + 1, romStorageBuffer)));
                    break;
            }
        }

        public void readValueFromPositionToList(ObservableCollection<GridRowVoltage> dest, int position, int type, String units = "", bool isFrequency = false)
        {
            switch (type)
            {
                case 0: // 16 bit value
                    dest.Add(new GridRowVoltage("0x" + position.ToString("X"), get16BitValueFromPosition(position, romStorageBuffer, isFrequency), units, "16-bit"));
                    break;
                case 1: // 24 bit value
                    dest.Add(new GridRowVoltage("0x" + position.ToString("X"), get24BitValueFromPosition(position, romStorageBuffer, isFrequency), units, "24-bit"));
                    break;
                case 2: // 8 bit value
                    dest.Add(new GridRowVoltage("0x" + position.ToString("X"), get8BitValueFromPosition(position, romStorageBuffer, isFrequency), units, "8-bit"));
                    break;
                default:
                    dest.Add(new GridRowVoltage("0x" + position.ToString("X"), get8BitValueFromPosition(position, romStorageBuffer, isFrequency), units, "8-bit"));
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
        // dumb way to extract 32 bit value (can be made much more effective but this is easy to read for anyone)
        public Int32 get32BitValueFromPosition(int position, byte[] buffer, bool isFrequency = false)
        {
            if (position < buffer.Length - 3)
            {
                if (isFrequency) // if its frequency divide by 100 to convert it into Mhz
                {
                    return (256 * 256 * 256 * buffer[position + 3]) + (256 * 256 * buffer[position + 2] + 256 * buffer[position + 1] + buffer[position]) / 100;
                }
                return (256 * 256 * 256 * buffer[position + 3]) + (256 * 256 * buffer[position + 2]) + (256 * buffer[position + 1]) + buffer[position];
            }
            return -1;
        }
        // dumb way to extract 16 bit value (can be made much more effective but this is easy to read for anyone)
        public Int32 get16BitValueFromPosition(int position, byte[] buffer, bool isFrequencyOrTemp = false)
        {
            if (position < buffer.Length - 1)
            {
                if (isFrequencyOrTemp) // if its frequency divide by 100 to convert it into Mhz
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

        private void SaveFileDialog_Click(object sender, RoutedEventArgs e)
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
                saveList(voltageList, true); // there are values which are not frequency but it works as they are only singlevalue
                saveList(memFrequencyList, true);
                saveList(gpuFrequencyList, true);
                saveList(gpumemFrequencyListAndPowerLimit, true);
                saveList(VCELimitTableData, false);
                saveList(ACPLimitTableData, false);
                saveList(UVDLimitTableData, false);
                saveList(SAMULimitTableData, false);
                saveList(fanList, true);
                fixChecksum(true);
                bw.Write(romStorageBuffer);

                fs.Close();
                bw.Close();
            }
        }
        private void fixChecksum(bool save)
        {
            Byte oldchecksum = romStorageBuffer[33];
            int size = romStorageBuffer[2] * 512;
            Byte newchecksum = 0;

            for (int i = 0; i < size; i++)
            {
                newchecksum += romStorageBuffer[i];
            }
            if (oldchecksum == (romStorageBuffer[33] - newchecksum))
            {
                checksumResult.Text = "OK";
            }
            else
            {
                checksumResult.Text = "WRONG - save for fix";
            }
            if (save)
            {
                romStorageBuffer[33] -= newchecksum;
                checksumResult.Text = "OK";
            }

        }

        private void saveList(ObservableCollection<GridRowVoltage> list, bool isFrequency = false)
        {
            foreach (GridRowVoltage row in list)
            {
                int savePosition;
                int value = row.value;
                if (row.position.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
                {
                    row.position = row.position.Substring(2);
                }
                if (isFrequency) // there is hack for 16 bit need fix
                {
                    value *= 100;
                }
                if (int.TryParse(row.position, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out savePosition))
                {
                    switch (row.type)
                    {
                        case "24-bit":
                            {
                                // this is for 24 bit
                                romStorageBuffer[savePosition] = (byte)value;
                                romStorageBuffer[savePosition + 1] = (byte)(value >> 8);
                                romStorageBuffer[savePosition + 2] = (byte)(value >> 16);
                                break;
                            }
                        case "16-bit":
                            {
                                // hack here to make it *100 for fan table should be fixed other way :D (we need to save isfrequencyortemp for each row)
                                if (list == fanList)
                                {
                                    romStorageBuffer[savePosition] = (byte)value;
                                    romStorageBuffer[savePosition + 1] = (byte)(value >> 8);
                                }
                                else
                                {
                                    romStorageBuffer[savePosition] = (byte)row.value;
                                    romStorageBuffer[savePosition + 1] = (byte)(row.value >> 8);
                                }
                                break;
                            }
                        case "8-bit":
                            {
                                romStorageBuffer[savePosition] = (byte)row.value;
                                break;
                            }
                    }
                }
            }
        }
        private void saveList(ObservableCollection<GridRow> list, bool isFrequency = false)
        {
            foreach (GridRow row in list)
            {
                int savePosition;
                int savePosition2;
                int value = row.value;
                int voltage = row.vol;
                if (row.position.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase) && row.posvol.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
                {
                    row.position = row.position.Substring(2);
                    row.posvol = row.posvol.Substring(2);
                }

                if (isFrequency) // there is hack for 16 bit need fix
                {
                    value *= 100;
                }
                if (int.TryParse(row.position, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out savePosition))
                {
                    switch (row.type)
                    {
                        case "24-bit":
                            {
                                // this is for 24 bit
                                romStorageBuffer[savePosition] = (byte)value;
                                romStorageBuffer[savePosition + 1] = (byte)(value >> 8);
                                romStorageBuffer[savePosition + 2] = (byte)(value >> 16);
                                break;
                            }
                        case "16-bit":
                            {
                                romStorageBuffer[savePosition] = (byte)row.value;
                                romStorageBuffer[savePosition + 1] = (byte)(row.value >> 8);
                                break;
                            }
                        case "8-bit":
                            {
                                romStorageBuffer[savePosition] = (byte)row.value;
                                break;
                            }
                    }
                }
                if (int.TryParse(row.posvol, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out savePosition2))
                {
                    romStorageBuffer[savePosition2] = (byte)row.vol;
                    romStorageBuffer[savePosition2 + 1] = (byte)(row.vol >> 8);
                }
            }
        }
        // this is here because of bug with tabs and grids thanks microsoft
        private void fanTable_GotFocus(object sender, RoutedEventArgs e)
        {
            fanTable.Columns[0].IsReadOnly = true;
            fanTable.Columns[1].IsReadOnly = false;
            fanTable.Columns[2].IsReadOnly = true;
            fanTable.Columns[3].IsReadOnly = true;
        }
        private void memoryTimingTable_GotFocus(object sender, RoutedEventArgs e)
        {
            memoryTimingTable.Columns[0].IsReadOnly = true;
            memoryTimingTable.Columns[1].IsReadOnly = false;
            memoryTimingTable.Columns[2].IsReadOnly = true;
            memoryTimingTable.Columns[3].IsReadOnly = true;
        }

        private void gpuFrequencyTable_GotFocus(object sender, RoutedEventArgs e)
        {
            gpuFrequencyTable.Columns[0].IsReadOnly = true;
            gpuFrequencyTable.Columns[1].IsReadOnly = true;
            gpuFrequencyTable.Columns[2].IsReadOnly = false;
            gpuFrequencyTable.Columns[3].IsReadOnly = true;
            gpuFrequencyTable.Columns[4].IsReadOnly = true;
            gpuFrequencyTable.Columns[5].IsReadOnly = true;
            gpuFrequencyTable.Columns[6].IsReadOnly = false;
        }

        private void memFrequencyTable_GotFocus(object sender, RoutedEventArgs e)
        {
            memFrequencyTable.Columns[0].IsReadOnly = true;
            memFrequencyTable.Columns[1].IsReadOnly = true;
            memFrequencyTable.Columns[2].IsReadOnly = false;
            memFrequencyTable.Columns[3].IsReadOnly = true;
            memFrequencyTable.Columns[4].IsReadOnly = true;
            gpuFrequencyTable.Columns[5].IsReadOnly = true;
            gpuFrequencyTable.Columns[6].IsReadOnly = false;
        }

        private void memgpuFrequencyTable_GotFocus(object sender, RoutedEventArgs e)
        {
            memgpuFrequencyTable.Columns[0].IsReadOnly = true;
            memgpuFrequencyTable.Columns[1].IsReadOnly = false;
            memgpuFrequencyTable.Columns[2].IsReadOnly = true;
            memgpuFrequencyTable.Columns[3].IsReadOnly = true;
        }

        private void VCELimitTable_GotFocus(object sender, RoutedEventArgs e)
        {
            VCELimitTable.Columns[0].IsReadOnly = true;
            VCELimitTable.Columns[1].IsReadOnly = true;
            VCELimitTable.Columns[2].IsReadOnly = true;
            VCELimitTable.Columns[3].IsReadOnly = true;
            VCELimitTable.Columns[4].IsReadOnly = true;
            VCELimitTable.Columns[5].IsReadOnly = true;
            VCELimitTable.Columns[6].IsReadOnly = false;
        }
        private void ACPLimitTable_GotFocus(object sender, RoutedEventArgs e)
        {
            ACPLimitTable.Columns[0].IsReadOnly = true;
            ACPLimitTable.Columns[1].IsReadOnly = true;
            ACPLimitTable.Columns[2].IsReadOnly = true;
            ACPLimitTable.Columns[3].IsReadOnly = true;
            ACPLimitTable.Columns[4].IsReadOnly = true;
            ACPLimitTable.Columns[5].IsReadOnly = true;
            ACPLimitTable.Columns[6].IsReadOnly = false;
        }
        private void SAMULimitTable_GotFocus(object sender, RoutedEventArgs e)
        {
            SAMULimitTable.Columns[0].IsReadOnly = true;
            SAMULimitTable.Columns[1].IsReadOnly = true;
            SAMULimitTable.Columns[2].IsReadOnly = true;
            SAMULimitTable.Columns[3].IsReadOnly = true;
            SAMULimitTable.Columns[4].IsReadOnly = true;
            SAMULimitTable.Columns[5].IsReadOnly = true;
            SAMULimitTable.Columns[6].IsReadOnly = false;
        }
        private void UVDLimitTable_GotFocus(object sender, RoutedEventArgs e)
        {
            UVDLimitTable.Columns[0].IsReadOnly = true;
            UVDLimitTable.Columns[1].IsReadOnly = true;
            UVDLimitTable.Columns[2].IsReadOnly = true;
            UVDLimitTable.Columns[3].IsReadOnly = true;
            UVDLimitTable.Columns[4].IsReadOnly = true;
            UVDLimitTable.Columns[5].IsReadOnly = true;
            UVDLimitTable.Columns[6].IsReadOnly = false;
        }
    }
}
