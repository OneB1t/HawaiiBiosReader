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
        int powerTablePosition; // start position of powertable in rom
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
                        gpuclock3.Text = position.ToString() +" -- "; 
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
                            somevalues.Text += get24BitValueFromPosition(position, buffer)  + System.Environment.NewLine;
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
                        for(int i = 0;i < 8;i++)
                        {
                            position = powerTablePosition + somevalue4offset + (i * 3);
                            somevalues4.Text += position.ToString() + "  -- ";
                            somevalues4.Text += buffer[position + 1] + "  -- ";
                            somevalues4.Text += buffer[position] + System.Environment.NewLine;
                        }

                    }
                    fileStream.Close();
                }
            }
        }
        
        public int PatternAt(byte[] source, byte[] pattern) // search for powertable pattern this way is slow but works fine
        {
            for (int i = 0; i < source.Length; i++)
            {
                if (source.Skip(i).Take(pattern.Length).SequenceEqual(pattern))
                {
                    return i;
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
