using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.Globalization;
using HawaiiBiosReader;
using System.Data;

namespace HawaiBiosReader
{

    public partial class MainWindow : Window
    {
        ObservableCollection<GridRow> data = new ObservableCollection<GridRow>();

        ObservableCollection<GenericGridRow> gpumemFrequencyListAndPowerLimit = new ObservableCollection<GenericGridRow>();
        ObservableCollection<GenericGridRow> fanList = new ObservableCollection<GenericGridRow>();
        ObservableCollection<GenericGridRow> vrmList = new ObservableCollection<GenericGridRow>();

        ObservableCollection<GridRow> vddciList = new ObservableCollection<GridRow>();
        ObservableCollection<GridRow> gpuFrequencyList = new ObservableCollection<GridRow>();
        ObservableCollection<GridRow> memFrequencyList = new ObservableCollection<GridRow>();
        ObservableCollection<GridRow> VCELimitTableData = new ObservableCollection<GridRow>();
        ObservableCollection<GridRow> UVDLimitTableData = new ObservableCollection<GridRow>();
        ObservableCollection<GridRow> SAMULimitTableData = new ObservableCollection<GridRow>();
        ObservableCollection<GridRow> ACPLimitTableData = new ObservableCollection<GridRow>();
        ObservableCollection<GridRow> VRMSettingsTableData = new ObservableCollection<GridRow>();

        Byte[] romStorageBuffer;

        string[] supportedDevIDs = new string[] { "67A0", "67A1", "67A2", "67A8", "67A9", "67AA", "67B0", "67B1", "67B9" };

        // unknown table offsets
        int headerPosition;
        int dataPointersPosition;
        int powerTablePosition;
        int powerTableSize;
        int fanTablePosition;
        int pciInfoPosition;
        int gpuVRMTablePosition;

        int clockInfoOffset;
        int memoryFrequencyTableOffset;
        int gpuFrequencyTableOffset;
        int limitsPointersOffset;
        int VCELimitTableOffset;
        int SAMULimitTableOffset;
        int ACPLimitTableOffset;
        int UVDLimitTableOffset;
        int tdpLimitOffset;
        int tdcLimitOffset;
        int powerDeliveryLimitOffset;
        int AUXvoltageOffset;


        // table offsets for default
        int biosNameOffset = 220;

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
                // Clear all the controls
                gpumemFrequencyListAndPowerLimit.Clear();
                vddciList.Clear();
                gpuMaxClock.Clear();
                memMaxClock.Clear();
                memFrequencyList.Clear();
                gpuFrequencyList.Clear();
                VCELimitTableData.Clear();
                UVDLimitTableData.Clear();
                SAMULimitTableData.Clear();
                ACPLimitTableData.Clear();
                fanList.Clear();
                VRMSettingsTableData.Clear();

                // Open the selected file to read.
                System.IO.Stream fileStream = openFileDialog.OpenFile();
                filename.Text = openFileDialog.FileName;

                using (BinaryReader br = new BinaryReader(fileStream)) // binary reader
                {
                    romStorageBuffer = br.ReadBytes((int)fileStream.Length);
                    fixChecksum(false);
                    pciInfoPosition = getNBitValueFromPosition(16, 24, romStorageBuffer);
                    headerPosition = getNBitValueFromPosition(16, 72, romStorageBuffer);
                    dataPointersPosition = getNBitValueFromPosition(16, headerPosition + 32, romStorageBuffer);
                    powerTablePosition = getNBitValueFromPosition(16, dataPointersPosition + 34, romStorageBuffer);
                    gpuVRMTablePosition = getNBitValueFromPosition(16, dataPointersPosition + 68, romStorageBuffer);

                    biosName.Text = getTextFromBinary(romStorageBuffer, biosNameOffset, 32);
                    string devIDstr = getNBitValueFromPosition(16, pciInfoPosition + 6, romStorageBuffer).ToString("X");
                    deviceID.Text = "0x" + devIDstr;
                    vendorID.Text = "0x" + getNBitValueFromPosition(16, pciInfoPosition + 4, romStorageBuffer).ToString("X");
                    productData.Text = "0x" + getNBitValueFromPosition(16, pciInfoPosition + 8, romStorageBuffer).ToString("X");
                    structureLenght.Text = "0x" + getNBitValueFromPosition(16, pciInfoPosition + 10, romStorageBuffer).ToString("X");
                    structureRevision.Text = "0x" + getNBitValueFromPosition(8, pciInfoPosition + 12, romStorageBuffer).ToString("X");
                    classCode.Text = "0x" + getNBitValueFromPosition(8, pciInfoPosition + 13, romStorageBuffer).ToString("X") + " - " + "0x" + getNBitValueFromPosition(8, pciInfoPosition + 14, romStorageBuffer).ToString("X") + " - " + "0x" + getNBitValueFromPosition(8, pciInfoPosition + 15, romStorageBuffer).ToString("X");
                    imageLenght.Text = "0x" + getNBitValueFromPosition(16, pciInfoPosition + 16, romStorageBuffer).ToString("X");
                    revisionLevel.Text = "0x" + getNBitValueFromPosition(16, pciInfoPosition + 18, romStorageBuffer).ToString("X");
                    codeType.Text = "0x" + getNBitValueFromPosition(8, pciInfoPosition + 20, romStorageBuffer).ToString("X");
                    indicator.Text = "0x" + getNBitValueFromPosition(8, pciInfoPosition + 21, romStorageBuffer).ToString("X");
                    reserved.Text = "0x" + getNBitValueFromPosition(16, pciInfoPosition + 22, romStorageBuffer).ToString("X");

                    if (!supportedDevIDs.Contains(devIDstr))
                    {
                        MessageBoxResult result = MessageBox.Show("Unsupported ROM", "Error", MessageBoxButton.OK);
                    }
                    else
                    {
                        powerTableSize = getNBitValueFromPosition(16, powerTablePosition, romStorageBuffer);
                        powerTablesize.Text = powerTableSize.ToString();
                        powerTablePositionValue.Text = "0x" + powerTablePosition.ToString("X");

                        /*#################################################################################################
                         * 
                         *               BIOS PARSING SECTION
                         * 
                        #################################################################################################*/

                        clockInfoOffset = getNBitValueFromPosition(16, powerTablePosition + 11, romStorageBuffer);
                        fanTablePosition = powerTablePosition + getNBitValueFromPosition(16, powerTablePosition + 42, romStorageBuffer);
                        gpuFrequencyTableOffset = getNBitValueFromPosition(16, powerTablePosition + 54, romStorageBuffer);
                        //TEMP (Only gets first VDDCI) - +4 to skip number of vddci states(1 byte) and first frequency(3 bytes)
                        AUXvoltageOffset = getNBitValueFromPosition(16, powerTablePosition + 56, romStorageBuffer);
                        memoryFrequencyTableOffset = getNBitValueFromPosition(16, powerTablePosition + 58, romStorageBuffer);

                        limitsPointersOffset = getNBitValueFromPosition(16, powerTablePosition + 44, romStorageBuffer);
                        int VCETableOffset = getNBitValueFromPosition(16, powerTablePosition + limitsPointersOffset + 10, romStorageBuffer);
                        int VCEunknownStatesNum = getNBitValueFromPosition(8, powerTablePosition + VCETableOffset + 1, romStorageBuffer);
                        VCELimitTableOffset = VCETableOffset + 2 + VCEunknownStatesNum * 6;
                        int UVDTableOffset = getNBitValueFromPosition(16, powerTablePosition + limitsPointersOffset + 12, romStorageBuffer);
                        int UVDunknownStatesNum = getNBitValueFromPosition(8, powerTablePosition + UVDTableOffset + 1, romStorageBuffer);
                        UVDLimitTableOffset = UVDTableOffset + 2 + UVDunknownStatesNum * 6;
                        SAMULimitTableOffset = getNBitValueFromPosition(16, powerTablePosition + limitsPointersOffset + 14, romStorageBuffer);
                        ACPLimitTableOffset = getNBitValueFromPosition(16, powerTablePosition + limitsPointersOffset + 18, romStorageBuffer);

                        tdpLimitOffset = getNBitValueFromPosition(16, powerTablePosition + limitsPointersOffset + 20, romStorageBuffer) + 3;
                        powerDeliveryLimitOffset = tdpLimitOffset + 12;
                        tdcLimitOffset = tdpLimitOffset + 2;

                        // OverDrive Limits
                        int CCCLimitsPosition = powerTablePosition + getNBitValueFromPosition(16, powerTablePosition + 44, romStorageBuffer);
                        readValueFromPosition(gpuMaxClock, CCCLimitsPosition + 2, 1, "Mhz", true);
                        readValueFromPosition(memMaxClock, CCCLimitsPosition + 6, 1, "Mhz", true);

                        // GPU, MEM, PL, VDDCI
                        gpumemFrequencyListAndPowerLimit.Add(new GenericGridRow("0x" + (powerTablePosition + clockInfoOffset + 2).ToString("X"), getNBitValueFromPosition(24, powerTablePosition + clockInfoOffset + 2, romStorageBuffer, true), "Mhz", "24-bit"));
                        gpumemFrequencyListAndPowerLimit.Add(new GenericGridRow("0x" + (powerTablePosition + clockInfoOffset + 11).ToString("X"), getNBitValueFromPosition(24, powerTablePosition + clockInfoOffset + 11, romStorageBuffer, true), "Mhz", "24-bit"));
                        gpumemFrequencyListAndPowerLimit.Add(new GenericGridRow("0x" + (powerTablePosition + clockInfoOffset + 20).ToString("X"), getNBitValueFromPosition(24, powerTablePosition + clockInfoOffset + 20, romStorageBuffer, true), "Mhz", "24-bit"));
                        gpumemFrequencyListAndPowerLimit.Add(new GenericGridRow("0x" + (powerTablePosition + clockInfoOffset + 5).ToString("X"), getNBitValueFromPosition(24, powerTablePosition + clockInfoOffset + 5, romStorageBuffer, true), "Mhz", "24-bit"));
                        gpumemFrequencyListAndPowerLimit.Add(new GenericGridRow("0x" + (powerTablePosition + clockInfoOffset + 14).ToString("X"), getNBitValueFromPosition(24, powerTablePosition + clockInfoOffset + 14, romStorageBuffer, true), "Mhz", "24-bit"));
                        gpumemFrequencyListAndPowerLimit.Add(new GenericGridRow("0x" + (powerTablePosition + clockInfoOffset + 23).ToString("X"), getNBitValueFromPosition(24, powerTablePosition + clockInfoOffset + 23, romStorageBuffer, true), "Mhz", "24-bit"));
                        gpumemFrequencyListAndPowerLimit.Add(new GenericGridRow("0x" + (powerTablePosition + tdpLimitOffset).ToString("X"), getNBitValueFromPosition(16, powerTablePosition + tdpLimitOffset, romStorageBuffer), "W", "16-bit"));
                        gpumemFrequencyListAndPowerLimit.Add(new GenericGridRow("0x" + (powerTablePosition + powerDeliveryLimitOffset).ToString("X"), getNBitValueFromPosition(16, powerTablePosition + powerDeliveryLimitOffset, romStorageBuffer), "W", "16-bit"));
                        gpumemFrequencyListAndPowerLimit.Add(new GenericGridRow("0x" + (powerTablePosition + tdcLimitOffset).ToString("X"), getNBitValueFromPosition(16, powerTablePosition + tdcLimitOffset, romStorageBuffer), "A", "16-bit"));
                        gpumemFrequencyListAndPowerLimit.Add(new GenericGridRow("0x" + (powerTablePosition + powerDeliveryLimitOffset + 2).ToString("X"), getNBitValueFromPosition(16, powerTablePosition + powerDeliveryLimitOffset + 2, romStorageBuffer), "°C", "16-bit"));
                        memgpuFrequencyTable.ItemsSource = gpumemFrequencyListAndPowerLimit;

                        // VDDCITable
                        int vddciTableCount = getNBitValueFromPosition(8, powerTablePosition + AUXvoltageOffset, romStorageBuffer);
                        for (int i = 0; i < vddciTableCount; i++)
                        {
                            readValueFromPositionToList(vddciList, (powerTablePosition + AUXvoltageOffset + 1 + (i * 5)), 1, "Mhz", true, i);
                        }
                        vddciTable.ItemsSource = vddciList;

                        // MEMFreqTable
                        int gpuFrequencyTableCount = getNBitValueFromPosition(8, powerTablePosition + memoryFrequencyTableOffset, romStorageBuffer);
                        for (int i = 0; i < gpuFrequencyTableCount; i++)
                        {
                            readValueFromPositionToList(memFrequencyList, (powerTablePosition + memoryFrequencyTableOffset + 1 + (i * 5)), 1, "Mhz", true, i);
                        }
                        memFrequencyTable.ItemsSource = memFrequencyList;

                        // GPUFreqTable
                        int memoryFrequencyTableCount = getNBitValueFromPosition(8, powerTablePosition + gpuFrequencyTableOffset, romStorageBuffer);
                        for (int i = 0; i < memoryFrequencyTableCount; i++)
                        {
                            readValueFromPositionToList(gpuFrequencyList, (powerTablePosition + gpuFrequencyTableOffset + 1 + (i * 5)), 1, "Mhz", true, i);
                        }
                        gpuFrequencyTable.ItemsSource = gpuFrequencyList;

                        int position = 0;
                        // StartVCELimitTable
                        for (int i = 0; i < getNBitValueFromPosition(8, powerTablePosition + VCELimitTableOffset, romStorageBuffer); i++)
                        {
                            position = powerTablePosition + VCELimitTableOffset + 1 + (i * 3);
                            VCELimitTableData.Add(new GridRow("0x" + (position + 2).ToString("X"), getNBitValueFromPosition(8, position + 2, romStorageBuffer), "DPM", "8-bit", i, "0x" + (position).ToString("X"), getNBitValueFromPosition(16, position, romStorageBuffer, false)));
                        }
                        VCELimitTable.ItemsSource = VCELimitTableData;

                        // StartUVDLimitTable
                        for (int i = 0; i < getNBitValueFromPosition(8, powerTablePosition + UVDLimitTableOffset, romStorageBuffer); i++)
                        {
                            position = powerTablePosition + UVDLimitTableOffset + 1 + (i * 3);
                            UVDLimitTableData.Add(new GridRow("0x" + (position + 2).ToString("X"), getNBitValueFromPosition(8, position + 2, romStorageBuffer), "DPM", "8-bit", i, "0x" + (position).ToString("X"), getNBitValueFromPosition(16, position, romStorageBuffer, false)));
                        }
                        UVDLimitTable.ItemsSource = UVDLimitTableData;

                        // StartSAMULimitTable
                        for (int i = 0; i < getNBitValueFromPosition(8, powerTablePosition + SAMULimitTableOffset + 1, romStorageBuffer); i++)
                        {
                            position = powerTablePosition + SAMULimitTableOffset + 2 + (i * 5);
                            SAMULimitTableData.Add(new GridRow("0x" + (position + 2).ToString("X"), getNBitValueFromPosition(24, position + 2, romStorageBuffer), "%", "24-bit", i, "0x" + (position).ToString("X"), getNBitValueFromPosition(16, position, romStorageBuffer, false)));
                        }
                        SAMULimitTable.ItemsSource = SAMULimitTableData;

                        // StartACPLimitTable
                        for (int i = 0; i < getNBitValueFromPosition(8, powerTablePosition + ACPLimitTableOffset + 1, romStorageBuffer); i++)
                        {
                            position = powerTablePosition + ACPLimitTableOffset + 2 + (i * 5);
                            ACPLimitTableData.Add(new GridRow("0x" + (position + 2).ToString("X"), getNBitValueFromPosition(24, position + 2, romStorageBuffer), "%", "24-bit", i, "0x" + (position).ToString("X"), getNBitValueFromPosition(16, position, romStorageBuffer, false)));
                        }
                        ACPLimitTable.ItemsSource = ACPLimitTableData;

                        // Fan list
                        fanList.Add(new GenericGridRow("0x" + (fanTablePosition + 1).ToString("X"), getNBitValueFromPosition(8, fanTablePosition + 1, romStorageBuffer), "°C", "8-bit")); //temperatureHysteresis
                        fanList.Add(new GenericGridRow("0x" + (fanTablePosition + 2).ToString("X"), getNBitValueFromPosition(16, fanTablePosition + 2, romStorageBuffer, true), "°C", "16-bit")); //fantemperature1
                        fanList.Add(new GenericGridRow("0x" + (fanTablePosition + 4).ToString("X"), getNBitValueFromPosition(16, fanTablePosition + 4, romStorageBuffer, true), "°C", "16-bit")); //fantemperature2
                        fanList.Add(new GenericGridRow("0x" + (fanTablePosition + 6).ToString("X"), getNBitValueFromPosition(16, fanTablePosition + 6, romStorageBuffer, true), "°C", "16-bit")); //fantemperature3
                        fanList.Add(new GenericGridRow("0x" + (fanTablePosition + 8).ToString("X"), getNBitValueFromPosition(16, fanTablePosition + 8, romStorageBuffer, true), "%", "16-bit")); //fanspeed1
                        fanList.Add(new GenericGridRow("0x" + (fanTablePosition + 10).ToString("X"), getNBitValueFromPosition(16, fanTablePosition + 10, romStorageBuffer, true), "%", "16-bit")); //fanspeed2
                        fanList.Add(new GenericGridRow("0x" + (fanTablePosition + 12).ToString("X"), getNBitValueFromPosition(16, fanTablePosition + 12, romStorageBuffer, true), "%", "16-bit")); //fanspeed3
                        fanList.Add(new GenericGridRow("0x" + (fanTablePosition + 14).ToString("X"), getNBitValueFromPosition(16, fanTablePosition + 14, romStorageBuffer, true), "°C", "16-bit")); //fantemperature4
                        fanList.Add(new GenericGridRow("0x" + (fanTablePosition + 16).ToString("X"), getNBitValueFromPosition(8, fanTablePosition + 16, romStorageBuffer), "1/0", "8-bit")); //fanControlType
                        fanList.Add(new GenericGridRow("0x" + (fanTablePosition + 17).ToString("X"), getNBitValueFromPosition(16, fanTablePosition + 17, romStorageBuffer), "%", "8-bit")); //pwmFanMax
                        fanTable.ItemsSource = fanList;
                        
                        // VRM list
                        

                        // this is here as hackfix to show which columns can be edited...
                        switch(tabControl1.SelectedIndex)
                        {
                            case 1:
                                memgpuFrequencyTable.UpdateLayout();
                                memgpuFrequencyTable.Focus();
                                memgpuFrequencyTable_GotFocus(null, null);
                                break;
                            case 2:
                                VCELimitTable.UpdateLayout();
                                VCELimitTable.Focus();
                                VCELimitTable_GotFocus(null, null);
                                break;
                            case 3:
                                fanTable.UpdateLayout();
                                fanTable.Focus();
                                fanTable_GotFocus(null, null);
                                break;
                            case 4:
                                VRMSettingTable.UpdateLayout();
                                VRMSettingTable.Focus();
                                vrmSettingsTable_GotFocus(null,null);
                                break;
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
                        dest.Text += (getNBitValueFromPosition(16, position, romStorageBuffer, isFrequency) * 6.25).ToString() + " " + units;
                    else
                        dest.Text += getNBitValueFromPosition(16, position, romStorageBuffer, isFrequency).ToString() + " " + units;
                    break;
                case 1: // 24 bit value
                    dest.Text += getNBitValueFromPosition(24, position, romStorageBuffer, isFrequency).ToString() + " " + units;
                    break;
                case 2: // 8 bit value
                    if (voltage)
                        dest.Text += (romStorageBuffer[position] * 6.25).ToString() + " " + units;
                    else
                        dest.Text += romStorageBuffer[position].ToString() + " " + units;
                    break;
                case 3: // 32 bit value
                    dest.Text += getNBitValueFromPosition(32, position, romStorageBuffer, isFrequency).ToString() + " " + units;
                    break;
                default:
                    dest.Text += getNBitValueFromPosition(16, position, romStorageBuffer, isFrequency).ToString() + " " + units;
                    break;
            }
        }

        public void readValueFromPositionToList(ObservableCollection<GridRow> dest, int position, int type, String units = "", bool isFrequency = false, int dpm = -1)
        {
            switch (type)
            {
                case 0: // 16 bit value
                    dest.Add(new GridRow("0x" + position.ToString("X"), getNBitValueFromPosition(16, position, romStorageBuffer, isFrequency), units, "16-bit", dpm, "0x" + (position + 2).ToString("X"), getNBitValueFromPosition(16, position + 2, romStorageBuffer)));
                    break;
                case 1: // 24 bit value
                    dest.Add(new GridRow("0x" + position.ToString("X"), getNBitValueFromPosition(24, position, romStorageBuffer, isFrequency), units, "24-bit", dpm, "0x" + (position + 3).ToString("X"), getNBitValueFromPosition(16, position + 3, romStorageBuffer)));
                    break;
                case 2: // 8 bit value
                    dest.Add(new GridRow("0x" + position.ToString("X"), getNBitValueFromPosition(8, position, romStorageBuffer, isFrequency), units, "8-bit", dpm, "0x" + (position + 1).ToString("X"), getNBitValueFromPosition(16, position + 1, romStorageBuffer)));
                    break;
                default:
                    dest.Add(new GridRow("0x" + position.ToString("X"), getNBitValueFromPosition(8, position, romStorageBuffer, isFrequency), units, "8-bit", dpm, "0x" + (position + 1).ToString("X"), getNBitValueFromPosition(16, position + 1, romStorageBuffer)));
                    break;
            }
        }

        public void readValueFromPositionToList(ObservableCollection<GenericGridRow> dest, int position, int type, String units = "", bool isFrequency = false)
        {
            switch (type)
            {
                case 0: // 16 bit value
                    dest.Add(new GenericGridRow("0x" + position.ToString("X"), getNBitValueFromPosition(16, position, romStorageBuffer, isFrequency), units, "16-bit"));
                    break;
                case 1: // 24 bit value
                    dest.Add(new GenericGridRow("0x" + position.ToString("X"), getNBitValueFromPosition(24, position, romStorageBuffer, isFrequency), units, "24-bit"));
                    break;
                case 2: // 8 bit value
                default:
                    dest.Add(new GenericGridRow("0x" + position.ToString("X"), getNBitValueFromPosition(8, position, romStorageBuffer, isFrequency), units, "8-bit"));
                    break;
            }
        }

        public String getTextFromBinary(byte[] binary, int offset, int lenght)
        {
            System.Text.Encoding encEncoder = System.Text.ASCIIEncoding.ASCII;
            string str = encEncoder.GetString(binary.Skip(offset).Take(lenght).ToArray());
            return str;
        }

        // dumb way to extract all values
        public Int32 getNBitValueFromPosition(int bits, int position, byte[] buffer, bool isFrequency = false)
        {
            int value = 0;
            if (position < buffer.Length - 2)
            {
                switch (bits)
                {
                    case 8:
                    default:
                        value = buffer[position];
                        break;
                    case 16:
                        value = (buffer[position + 1] << 8) | buffer[position];
                        break;
                    case 24:
                        value = (buffer[position + 2] << 16) | (buffer[position + 1] << 8) | buffer[position];
                        break;
                    case 32:
                        value = (buffer[position + 3] << 24) | (buffer[position + 2] << 16) | (buffer[position + 1] << 8) | buffer[position];
                        break;
                }

                if (isFrequency) // if its frequency divide by 100 to convert it into Mhz
                {
                    return value / 100;
                }
                return value;
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
                saveList(gpumemFrequencyListAndPowerLimit, true);
                saveList(vddciList, true);
                saveList(memFrequencyList, true);
                saveList(gpuFrequencyList, true);
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
                checksumResult.Foreground = Brushes.Green;
                checksumResult.Text = "OK";
            }
            else
            {
                checksumResult.Foreground = Brushes.Red;
                checksumResult.Text = "WRONG - save for fix";
            }
            if (save)
            {
                romStorageBuffer[33] -= newchecksum;
                checksumResult.Foreground = Brushes.Green;
                checksumResult.Text = "OK - Saved";

            }

        }

        private void saveList(ObservableCollection<GenericGridRow> list, bool isFrequency = false)
        {
            foreach (GenericGridRow row in list)
            {
                int savePosition;
                int value = row.Value;
                if (row.Address.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
                {
                    row.Address = row.Address.Substring(2);
                }
                if (isFrequency) // there is hack for 16 bit need fix
                {
                    value *= 100;
                }
                if (int.TryParse(row.Address, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out savePosition))
                {
                    switch (row.Length)
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
                                    romStorageBuffer[savePosition] = (byte)row.Value;
                                    romStorageBuffer[savePosition + 1] = (byte)(row.Value >> 8);
                                }
                                break;
                            }
                        case "8-bit":
                            {
                                romStorageBuffer[savePosition] = (byte)row.Value;
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
                int value = row.Value;
                int voltage = row.Voltage;
                if (row.Address.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase) && row.AdrVoltage.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
                {
                    row.Address = row.Address.Substring(2);
                    row.AdrVoltage = row.AdrVoltage.Substring(2);
                }

                if (isFrequency) // there is hack for 16 bit need fix
                {
                    value *= 100;
                }
                if (int.TryParse(row.Address, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out savePosition))
                {
                    switch (row.Length)
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
                                romStorageBuffer[savePosition] = (byte)row.Value;
                                romStorageBuffer[savePosition + 1] = (byte)(row.Value >> 8);
                                break;
                            }
                        case "8-bit":
                            {
                                romStorageBuffer[savePosition] = (byte)row.Value;
                                break;
                            }
                    }
                }
                if (int.TryParse(row.AdrVoltage, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out savePosition2))
                {
                    romStorageBuffer[savePosition2] = (byte)row.Voltage;
                    romStorageBuffer[savePosition2 + 1] = (byte)(row.Voltage >> 8);
                }
            }
        }
        void colorColumn(DataGrid datagrid, int columnIndex)
        {
            for (int i = 0; i < datagrid.Items.Count; i++)
            {
                DataGridRow firstRow = datagrid.ItemContainerGenerator.ContainerFromItem(datagrid.Items[i]) as DataGridRow;
                if (firstRow != null)
                {
                    DataGridCell entireColumn = datagrid.Columns[columnIndex].GetCellContent(firstRow).Parent as DataGridCell;
                    entireColumn.Background = Brushes.LightGreen;
                }
            }
        }
        void synchronizeValue(DataGridRowEditEndingEventArgs e, DataGrid grid)
        {
            Action action = delegate
            {
                int voltage = (e.Row.Item as GridRow).Voltage;
                foreach (GridRow row in grid.Items)
                {
                    if (row.DPM == (e.Row.DataContext as GridRow).DPM)
                    {
                        row.Voltage = voltage;
                        grid.Items.Refresh();
                        break;
                    }
                }                
            };
            Dispatcher.BeginInvoke(action, System.Windows.Threading.DispatcherPriority.Background);

        }
        // this is here because of bug with tabs and grids thanks microsoft
        private void fanTable_GotFocus(object sender, RoutedEventArgs e)
        {
            fanTable.Columns[0].IsReadOnly = true;
            fanTable.Columns[1].IsReadOnly = false;
            fanTable.Columns[2].IsReadOnly = true;
            fanTable.Columns[3].IsReadOnly = true;
            colorColumn(fanTable, 1);
        }
        private void vrmSettingsTable_GotFocus(object sender, RoutedEventArgs e)
        {
            fanTable.Columns[0].IsReadOnly = true;
            fanTable.Columns[1].IsReadOnly = false;
            fanTable.Columns[2].IsReadOnly = true;
            fanTable.Columns[3].IsReadOnly = true;
            colorColumn(VRMSettingTable, 1);
        }

        private void vddciTable_GotFocus(object sender, RoutedEventArgs e)
        {
            vddciTable.Columns[0].IsReadOnly = true;
            vddciTable.Columns[1].IsReadOnly = true;
            vddciTable.Columns[2].IsReadOnly = false;
            vddciTable.Columns[3].IsReadOnly = true;
            vddciTable.Columns[4].IsReadOnly = true;
            vddciTable.Columns[5].IsReadOnly = true;
            vddciTable.Columns[6].IsReadOnly = false;
            colorColumn(vddciTable, 2);
            colorColumn(vddciTable, 6);

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
            colorColumn(gpuFrequencyTable, 2);
            colorColumn(gpuFrequencyTable, 6);
        }

        private void memFrequencyTable_GotFocus(object sender, RoutedEventArgs e)
        {
            memFrequencyTable.Columns[0].IsReadOnly = true;
            memFrequencyTable.Columns[1].IsReadOnly = true;
            memFrequencyTable.Columns[2].IsReadOnly = false;
            memFrequencyTable.Columns[3].IsReadOnly = true;
            memFrequencyTable.Columns[4].IsReadOnly = true;
            memFrequencyTable.Columns[5].IsReadOnly = true;
            memFrequencyTable.Columns[6].IsReadOnly = false;
            colorColumn(memFrequencyTable, 2);
            colorColumn(memFrequencyTable, 6);
        }

        private void memgpuFrequencyTable_GotFocus(object sender, RoutedEventArgs e)
        {
            memgpuFrequencyTable.Columns[0].IsReadOnly = true;
            memgpuFrequencyTable.Columns[1].IsReadOnly = false;
            memgpuFrequencyTable.Columns[2].IsReadOnly = true;
            memgpuFrequencyTable.Columns[3].IsReadOnly = true;
            colorColumn(memgpuFrequencyTable, 1);
            memFrequencyTable_GotFocus(sender, e);
            gpuFrequencyTable_GotFocus(sender, e);
            vddciTable_GotFocus(sender, e);
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
            colorColumn(VCELimitTable, 6);
            ACPLimitTable_GotFocus(sender, e);
            SAMULimitTable_GotFocus(sender, e);
            UVDLimitTable_GotFocus(sender, e);
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
            colorColumn(ACPLimitTable, 6);
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
            colorColumn(SAMULimitTable, 6);
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
            colorColumn(UVDLimitTable, 6);
        }
        private void gpuFrequencyTable_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            synchronizeValue(e,memFrequencyTable);
        }

        private void memFrequencyTable_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            synchronizeValue(e, gpuFrequencyTable);
        }
    }
}
