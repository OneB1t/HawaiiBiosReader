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
        Byte[] buffer; // whole rom
        Byte[] PowerTablepattern = new byte[] { 0x03, 0xe8, 0x03, 0x58 }; // pattern to search for in buffer
        Byte[] FanControlpattern = new byte[] { 0x07, 0x06, 0x7C, 0x15 }; // pattern to search for in buffer
        Byte[] FanControl2pattern = new byte[] { 0x03, 0x06, 0x7C, 0x15 }; // pattern to search for in buffer
        int powerTablePosition; // start position of powertable in rom
        int fanTablePosition;
        int voltagetableoffset = 319; // 290 have different voltagetable offset than 390
        int memoryfrequencytableoffset = 278;
        int gpufrequencytableoffset = 231;
        int somevalueoffset = 396;
        int somevalue2offset = 549;
        int somevalue4offset = 441;


        public MainWindow()
        {
            InitializeComponent();
        }

        private void bOpenFileDialog_Click(object sender, RoutedEventArgs e)
        {
            // Create an instance of the open file dialog box.
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            // Set filter options and filter index.
            openFileDialog1.Filter = "Bios files (.rom)|*.rom|All Files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.Multiselect = false;

            // Call the ShowDialog method to show the dialog box.
            bool? userClickedOK = openFileDialog1.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOK == true)
            {
                // Open the selected file to read.
                System.IO.Stream fileStream = openFileDialog1.OpenFile();
                filename.Text = openFileDialog1.FileName;

                using (BinaryReader br = new BinaryReader(fileStream)) // binary reader
                {
                    buffer = br.ReadBytes((int)fileStream.Length);
                    powerTablePosition = PatternAt(buffer, PowerTablepattern);
                    fanTablePosition = PatternAt(buffer, FanControlpattern);
                    if (fanTablePosition == 0)
                    {
                        fanTablePosition = PatternAt(buffer, FanControl2pattern);
                        if (fanTablePosition == 0)
                        {
                            // no fan table found
                        }
                    }

                    if (powerTablePosition == 0)
                    {
                        MessageBoxResult result = MessageBox.Show("PowerTable position not found in this file", "Error", MessageBoxButton.OK);
                    }
                    else
                    {
                        powerTablePosition -= 16; // start of table is 16 bits from pattern i search for
                        int pom = buffer[powerTablePosition];
                        int pom2 = buffer[powerTablePosition + 1];
                        int tablesize = 256 * pom2 + pom;
                        powerTablesize.Text = tablesize.ToString();


                        /*#################################################################################################
                         * 
                         *               BIOS PARSING SECTION
                         * 
                        #################################################################################################*/
                        switch (tablesize)
                        {
                            case 660:
                                powerTablesize.Text = powerTablesize.Text + " - R9 390/390X";
                                voltagetableoffset = 319;
                                memoryfrequencytableoffset = 278;
                                gpufrequencytableoffset = 231;
                                somevalueoffset = 396;
                                somevalue2offset = 549;
                                somevalue4offset = 441;
                                break;
                            case 648:
                                powerTablesize.Text = powerTablesize.Text + " - R9 290/290X";
                                voltagetableoffset = 307;
                                memoryfrequencytableoffset = 266;
                                gpufrequencytableoffset = 219;
                                somevalueoffset = 384;
                                somevalue2offset = 537;
                                somevalue4offset = 429;
                                break;
                            case 658: // Slith mining bios for 290/290X
                                powerTablesize.Text = powerTablesize.Text + " - R9 290/290X The Stilt mining bios";
                                voltagetableoffset = 316;
                                memoryfrequencytableoffset = 275;
                                gpufrequencytableoffset = 228;
                                somevalueoffset = 394;
                                somevalue2offset = 547;
                                somevalue4offset = 439;
                                break;
                            case 642:
                                powerTablesize.Text = powerTablesize.Text + " - PT1/PT3 bios";
                                break;
                            default:
                                powerTablesize.Text = powerTablesize.Text + " - Unknown type";
                                break;

                        }

                        tbResults.Text = powerTablePosition.ToString();
                        powerTable.Text = returnTextFromBinary(buffer, powerTablePosition, tablesize);


                        // gpu clock1
                        int position = powerTablePosition + 98; // helper for position
                        gpuclock1.Text = position.ToString() + " -- ";
                        gpuclock1.Text += get24BitValueFromPosition(position, buffer, true).ToString() + " Mhz";

                        // gpu clock 2
                        position = powerTablePosition + 107;
                        gpuclock2.Text = position.ToString() + " -- ";
                        gpuclock2.Text += get24BitValueFromPosition(position, buffer, true).ToString() + " Mhz";

                        // gpu clock 3
                        position = powerTablePosition + 116;
                        gpuclock3.Text = position.ToString() + " -- ";
                        gpuclock3.Text += get24BitValueFromPosition(position, buffer, true).ToString() + " Mhz";

                        // mem clock 1
                        position = powerTablePosition + 101;
                        memclock1.Text = position.ToString() + " -- ";
                        memclock1.Text += get24BitValueFromPosition(position, buffer, true).ToString() + " Mhz";
                        // mem clock 2
                        position = powerTablePosition + 110;
                        memclock2.Text = position.ToString() + " -- ";
                        memclock2.Text += get24BitValueFromPosition(position, buffer, true).ToString() + " Mhz";
                        // mem clock 3
                        position = powerTablePosition + 119;
                        memclock3.Text = position.ToString() + " -- ";
                        memclock3.Text += get24BitValueFromPosition(position, buffer, true).ToString() + " Mhz";

                        // read voltage table
                        voltagetable.Text = "";
                        for (int i = 0; i < 24; i++)
                        {
                            position = powerTablePosition + voltagetableoffset + (i * 2);
                            voltagetable.Text += position.ToString() + " -- ";
                            voltagetable.Text += get16BitValueFromPosition(position, buffer) + " mV" + System.Environment.NewLine;
                        }

                        // memory frequency table?
                        memfrequencytable.Text = "";
                        for (int i = 0; i < 8; i++)
                        {
                            position = powerTablePosition + memoryfrequencytableoffset + (i * 5);
                            memfrequencytable.Text += position.ToString() + " -- ";
                            memfrequencytable.Text += get24BitValueFromPosition(position, buffer, true) + " Mhz" + System.Environment.NewLine;
                        }

                        // gpu frequency table?
                        gpufrequencytable.Text = "";
                        for (int i = 0; i < 8; i++)
                        {
                            position = powerTablePosition + gpufrequencytableoffset + (i * 5);
                            gpufrequencytable.Text += position.ToString() + " -- ";
                            gpufrequencytable.Text += get24BitValueFromPosition(position, buffer, true) + " Mhz" + System.Environment.NewLine;
                        }

                        // StartVCELimitTable
                        somevalues.Text = "";
                        for (int i = 0; i < 7; i++)
                        {
                            position = powerTablePosition + somevalueoffset + (i * 3);
                            somevalues.Text += position.ToString() + "  -- ";
                            somevalues.Text += i.ToString() + "  -- ";
                            somevalues.Text += get24BitValueFromPosition(position, buffer) + System.Environment.NewLine;
                        }

                        // StartSAMULimitTable + StartACPLimitTable
                        somevalues2.Text = "";
                        somevalues3.Text = "";
                        for (int i = 0; i < 16; i++)
                        {
                            if (i <= 7)
                            {
                                position = powerTablePosition + somevalue2offset + (i * 5);
                                somevalues2.Text += position.ToString() + "  -- ";
                                somevalues2.Text += get16BitValueFromPosition(position - 2, buffer) + "  -- ";
                                somevalues2.Text += get24BitValueFromPosition(position, buffer) + System.Environment.NewLine;
                            }
                            else
                            {
                                position = powerTablePosition + somevalue2offset + 2 + (i * 5);
                                somevalues3.Text += position.ToString() + "  -- ";
                                somevalues3.Text += get16BitValueFromPosition(position - 2, buffer) + "  -- ";
                                somevalues3.Text += get24BitValueFromPosition(position, buffer) + System.Environment.NewLine;
                            }
                        }

                        // StartUVDLimitTable
                        somevalues4.Text = "";
                        for (int i = 0; i < 8; i++)
                        {
                            position = powerTablePosition + somevalue4offset + (i * 3);
                            somevalues4.Text += position.ToString() + "  -- ";
                            somevalues4.Text += buffer[position + 1] + "  -- ";
                            somevalues4.Text += buffer[position] + System.Environment.NewLine;
                        }
                        if (fanTablePosition > 0)
                        {
                            position = fanTablePosition + 2;
                            fantemperature1.Text = position.ToString() + " -- ";
                            fantemperature1.Text += get16BitValueFromPosition(position, buffer, true).ToString() + " °C";

                            position = fanTablePosition + 4;
                            fantemperature2.Text = position.ToString() + " -- ";
                            fantemperature2.Text += get16BitValueFromPosition(position, buffer, true).ToString() + " °C";

                            position = fanTablePosition + 6;
                            fantemperature3.Text = position.ToString() + " -- ";
                            fantemperature3.Text += get16BitValueFromPosition(position, buffer, true).ToString() + " °C";

                            position = fanTablePosition + 8;
                            fanspeed1.Text = position.ToString() + " -- ";
                            fanspeed1.Text += get16BitValueFromPosition(position, buffer, true).ToString() + " %";

                            position = fanTablePosition + 10;
                            fanspeed2.Text = position.ToString() + " -- ";
                            fanspeed2.Text += get16BitValueFromPosition(position, buffer, true).ToString() + " %";

                            position = fanTablePosition + 12;
                            fanspeed3.Text = position.ToString() + " -- ";
                            fanspeed3.Text += get16BitValueFromPosition(position, buffer, true).ToString() + " %";
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


        private static int PatternAt(byte[] data, byte[] pattern)
        {
            if (pattern.Length > data.Length)
            {
                return -1;
            }
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

        public String returnTextFromBinary(byte[] binary, int offset, int lenght)
        {
            String result = "";
            for (int i = offset; i < offset + lenght; i++)
            {
                result += binary[i].ToString();
            }
            return result;

        }
        public Int32 get24BitValueFromPosition(int position, byte[] buffer, bool isfrequency = false) // dumb way to extract 24 bit value (can be made much more effective but this is easy to read for anyone)
        {
            int a = buffer[position];
            int b = buffer[position + 1];
            int c = buffer[position + 2];
            int result = 256 * 256 * c + 256 * b + a;
            if (isfrequency) // if its frequency divide by 100 to convert it into Mhz
            {
                return result / 100;
            }
            return result;
        }
        public Int32 get16BitValueFromPosition(int position, byte[] buffer, bool isfrequency = false)
        {
            int a = buffer[position];
            int b = buffer[position + 1];
            int result = 256 * b + a;
            if (isfrequency) // if its frequency divide by 100 to convert it into Mhz
            {
                return result / 100;
            }
            return result;
        }

    }
}
