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

namespace HawaiBiosReader
{
    public partial class MainWindow : Window
    {
        Byte[] romStorageBuffer; // whole rom
        Byte[] powerTablepattern = new byte[] { 0x02, 0x06, 0x01, 0x00 };
        int powerTablePosition; // start position of powertable in rom
        int fanTablePosition;
        int voltagetableoffset = 319; // 290 have different voltagetable offset than 390
        int memoryfrequencytableoffset = 278;
        int gpufrequencytableoffset = 231;
        int VCELimitTableOffset = 396;
        int AMUAndACPLimitTableOffset = 549;
        int UVDLimitTableOffset = 441;


        public MainWindow()
        {
            InitializeComponent();
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
                    fanTablePosition = powerTablePosition + 175;

                    if (powerTablePosition == -1)
                    {
                        MessageBoxResult result = MessageBox.Show("PowerTable position not found in this file", "Error", MessageBoxButton.OK);
                    }
                    else
                    {
                        int powerTableSize = 256 * romStorageBuffer[powerTablePosition + 1] + romStorageBuffer[powerTablePosition];
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
                                voltagetableoffset = 319;
                                memoryfrequencytableoffset = 278;
                                gpufrequencytableoffset = 231;
                                VCELimitTableOffset = 396;
                                AMUAndACPLimitTableOffset = 549;
                                UVDLimitTableOffset = 441;
                                break;
                            case 648:
                                powerTablesize.Text += " - R9 290/290X";
                                voltagetableoffset = 307;
                                memoryfrequencytableoffset = 266;
                                gpufrequencytableoffset = 219;
                                VCELimitTableOffset = 384;
                                AMUAndACPLimitTableOffset = 537;
                                UVDLimitTableOffset = 429;
                                break;
                            case 658: // Slith mining bios for 290/290X
                                powerTablesize.Text += " - R9 290/290X The Stilt mining bios";
                                voltagetableoffset = 316;
                                memoryfrequencytableoffset = 275;
                                gpufrequencytableoffset = 228;
                                VCELimitTableOffset = 394;
                                AMUAndACPLimitTableOffset = 547;
                                UVDLimitTableOffset = 439;
                                break;
                            case 642: // PT1/PT3
                                powerTablesize.Text += " - PT1/PT3 bios";
                                voltagetableoffset = 300;
                                memoryfrequencytableoffset = 259;
                                gpufrequencytableoffset = 212;
                                VCELimitTableOffset = 378;
                                AMUAndACPLimitTableOffset = 531;
                                UVDLimitTableOffset = 423;
                                break;
                            default:
                                powerTablesize.Text = powerTablesize.Text + " - Unknown type";
                                break;

                        }

                        tbResults.Text = powerTablePosition.ToString();
                        powerTable.Text = returnTextFromBinary(romStorageBuffer, powerTablePosition, powerTableSize);

                        int position = 0;
                        // gpu clock1
                        readValueFromPosition(gpuclock1, powerTablePosition + 98, 1, "Mhz", true);
                        // gpu clock 2
                        readValueFromPosition(gpuclock2, powerTablePosition + 107, 1, "Mhz", true);
                        // gpu clock 3
                        readValueFromPosition(gpuclock3, powerTablePosition + 116, 1, "Mhz", true);

                        // mem clock 1
                        readValueFromPosition(memclock1, powerTablePosition + 101, 1, "Mhz", true);
                        // mem clock 2
                        readValueFromPosition(memclock2, powerTablePosition + 110, 1, "Mhz", true);
                        // mem clock 3
                        readValueFromPosition(memclock3, powerTablePosition + 119, 1, "Mhz", true);

                        // read voltage table
                        voltagetable.Text = "";
                        for (int i = 0; i < 24; i++)
                        {
                            readValueFromPosition(voltagetable, powerTablePosition + voltagetableoffset + (i * 2), 0, "mV" + System.Environment.NewLine, false,true);
                        }

                        // memory frequency table
                        memfrequencytable.Text = "";
                        for (int i = 0; i < 8; i++)
                        {
                            readValueFromPosition(memfrequencytable, powerTablePosition + memoryfrequencytableoffset + (i * 5), 1, "Mhz" + System.Environment.NewLine, true, true);
                        }

                        // gpu frequency table
                        gpufrequencytable.Text = "";
                        for (int i = 0; i < 8; i++)
                        {
                            readValueFromPosition(gpufrequencytable, powerTablePosition + gpufrequencytableoffset + (i * 5), 1, "Mhz" + System.Environment.NewLine, true, true);
                        }

                        // StartVCELimitTable
                        somevalues.Text = "";
                        for (int i = 0; i < 7; i++)
                        {
                            position = powerTablePosition + VCELimitTableOffset + (i * 3);
                            somevalues.Text += position.ToString() + "  -- ";
                            somevalues.Text += i.ToString() + "  -- ";
                            somevalues.Text += get24BitValueFromPosition(position, romStorageBuffer) + System.Environment.NewLine;
                        }

                        // StartSAMULimitTable + StartACPLimitTable
                        AMULimitTable.Text = "";
                        ACPLimitTable.Text = "";
                        for (int i = 0; i < 16; i++)
                        {
                            if (i <= 7)
                            {
                                position = powerTablePosition + AMUAndACPLimitTableOffset + (i * 5);
                                AMULimitTable.Text += position.ToString() + "  -- ";
                                AMULimitTable.Text += get16BitValueFromPosition(position - 2, romStorageBuffer) + "  -- ";
                                AMULimitTable.Text += get24BitValueFromPosition(position, romStorageBuffer) + System.Environment.NewLine;
                            }
                            else
                            {
                                position = powerTablePosition + AMUAndACPLimitTableOffset + 2 + (i * 5);
                                ACPLimitTable.Text += position.ToString() + "  -- ";
                                ACPLimitTable.Text += get16BitValueFromPosition(position - 2, romStorageBuffer) + "  -- ";
                                ACPLimitTable.Text += get24BitValueFromPosition(position, romStorageBuffer) + System.Environment.NewLine;
                            }
                        }

                        // StartUVDLimitTable
                        UVDLimitTable.Text = "";
                        for (int i = 0; i < 8; i++)
                        {
                            position = powerTablePosition + UVDLimitTableOffset + (i * 3);
                            UVDLimitTable.Text += position.ToString() + "  -- ";
                            UVDLimitTable.Text += romStorageBuffer[position + 1] + "  -- ";
                            UVDLimitTable.Text += romStorageBuffer[position] + System.Environment.NewLine;
                        }
                        if (fanTablePosition > 0)
                        {
                            readValueFromPosition(fantemperature1, fanTablePosition + 2, 0, "C°",true);
                            readValueFromPosition(fantemperature2, fanTablePosition + 4, 0, "C°",true);
                            readValueFromPosition(fantemperature3, fanTablePosition + 6, 0, "C°",true);
                            readValueFromPosition(fantemperature4, fanTablePosition + 14, 0, "C°",true);

                            readValueFromPosition(fanspeed1, fanTablePosition + 8, 0, "%",true);
                            readValueFromPosition(fanspeed2, fanTablePosition + 10, 0, "%",true);
                            readValueFromPosition(fanspeed3, fanTablePosition + 12, 0, "%",true);
                        }
                        else
                        {
                            fanspeed1.Text = "NOT FOUND";
                            fanspeed2.Text = "NOT FOUND";
                            fanspeed3.Text = "NOT FOUND";
                            fantemperature1.Text = "NOT FOUND";
                            fantemperature2.Text = "NOT FOUND";
                            fantemperature3.Text = "NOT FOUND";
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
                dest.Text += position.ToString() + " -- ";
            }
            else
            {
                dest.Text = position.ToString() + " -- ";
            }

            switch (type)
            {
                case 0:
                    dest.Text += get16BitValueFromPosition(position, romStorageBuffer, isFrequency).ToString() + " " + units;
                    break;
                case 1:
                    dest.Text += get24BitValueFromPosition(position, romStorageBuffer, isFrequency).ToString() + " " + units;
                    break;
                default:
                    dest.Text += get16BitValueFromPosition(position, romStorageBuffer, isFrequency).ToString() + " " + units;
                    break;
            }
        }
        private static int PTPatternAt(byte[] data, byte[] pattern)
        {
            for (int di = 0; di < data.Length; di++)
            {
                if (data[di] == pattern[0] && data[di + 1] == pattern[1] && data[di + 2] == pattern[2] && data[di + 3] == pattern[3])
                {
                    Console.WriteLine("Found PT start point: " + di);
                    return di - 1;
                }
            }
            return 0;
        }

        public String returnTextFromBinary(byte[] binary, int offset, int lenght)
        {
            String result = "";
            for (int i = offset; i < offset + lenght; i++)
            {
                result += binary[i].ToString();
            }
            return result;
        }

        public Int32 get24BitValueFromPosition(int position, byte[] buffer, bool isFrequency = false) // dumb way to extract 24 bit value (can be made much more effective but this is easy to read for anyone)
        {
            int a = buffer[position];
            int b = buffer[position + 1];
            int c = buffer[position + 2];
            int result = 256 * 256 * c + 256 * b + a;
            if (isFrequency) // if its frequency divide by 100 to convert it into Mhz
            {
                return result / 100;
            }
            return result;
        }

        public Int32 get16BitValueFromPosition(int position, byte[] buffer, bool isFrequency = false)
        {
            int a = buffer[position];
            int b = buffer[position + 1];
            int result = 256 * b + a;
            if (isFrequency) // if its frequency divide by 100 to convert it into Mhz
            {
                return result / 100;
            }
            return result;
        }

    }
}
