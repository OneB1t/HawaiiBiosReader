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
        short voltagetableoffset = 319; // 290 have different voltagetable offset than 390
        
        
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
                                break;
                            case 648:
                                powerTablesize.Text = powerTablesize.Text + " - R9 290/290X";
                                voltagetableoffset = 307;
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
                        // gpu clock
                        gpuclock1.Text = get24BitValueFromPosition(powerTablePosition + 98, buffer, true).ToString() + " Mhz";
                        gpuclock2.Text = get24BitValueFromPosition(powerTablePosition + 107, buffer, true).ToString() + " Mhz";
                        gpuclock3.Text = get24BitValueFromPosition(powerTablePosition + 116, buffer, true).ToString() + " Mhz";

                        /// mem clock
                        memclock.Text = get24BitValueFromPosition(powerTablePosition + 101, buffer, true).ToString() + " Mhz";
                        memlowclock.Text = get24BitValueFromPosition(powerTablePosition + 110, buffer, true).ToString() + " Mhz";
                        memsaveclock.Text = get24BitValueFromPosition(powerTablePosition + 119, buffer, true).ToString() + " Mhz";

                        // read voltage table
                        voltagetable.Text = "";
                        for (int i = 0; i < 24; i++)
                        {
                            voltagetable.Text += get16BitValueFromPosition(powerTablePosition + voltagetableoffset + (i * 2), buffer, false) + " mV" + System.Environment.NewLine;
                        }
                        // memory frequency table?
                        frequencytable.Text = "";
                        for (int i = 0; i < 8; i++)
                        {
                            frequencytable.Text += get24BitValueFromPosition(powerTablePosition + 278 + (i * 5), buffer, false) + " Mhz" + System.Environment.NewLine;
                        }

                        // some values :D
                        somevalues.Text = "";
                        for (int i = 0; i < 14; i++)
                        {
                            somevalues.Text += get24BitValueFromPosition(powerTablePosition + 396 + (i * 3), buffer, false) + " DUNNO" + System.Environment.NewLine;
                        }

                        // some other values?
                        somevalues2.Text = "";
                        for (int i = 0; i < 16; i++)
                        {
                            if (i <= 7)
                            {
                                if (i == 0)
                                {
                                    somevalues2.Text += "1  -- "; // this value is not in table but it seems to be 1 (maybe need correction)
                                }
                                else
                                {
                                    somevalues2.Text += buffer[powerTablePosition + 547 + (i * 5)] + "  -- ";
                                }
                                somevalues2.Text += get24BitValueFromPosition(powerTablePosition + 549 + (i * 5), buffer, false) + " DUNNO" + System.Environment.NewLine;
                            }
                            else
                            {
                                if (i == 8)
                                {
                                    somevalues2.Text += "1  -- "; // this value is not in table but it seems to be 1 (maybe need correction)
                                }
                                else
                                {
                                    somevalues2.Text += buffer[powerTablePosition + 549 + (i * 5)] + "  -- ";
                                }
                                somevalues2.Text += get24BitValueFromPosition(powerTablePosition + 551 + (i * 5), buffer, false) + " DUNNO" + System.Environment.NewLine;
                            }
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
