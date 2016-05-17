using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HawaiiBiosReader
{
    public class GridRow
    {
        private String _position;
        private int _value;
        private String _unit;
        private String _type;
        private int _dpm;
        private String _posvoltage;
        private int _voltage;

        public GridRow(String pos, int val, String un, String type, int dpm)
        {
            _position = pos;
            _value = val;
            _unit = un;
            _type = type;
            _dpm = dpm;
        }

        public GridRow(String pos, int val, String un, String type, int dpm, String pos2, int voltage)
        {
            _position = pos;
            _value = val;
            _unit = un;
            _type = type;
            _dpm = dpm;
            _posvoltage = pos2;
            _voltage = voltage;
        }

        public int DPM
        {
            get { return _dpm; }
            set { _dpm = value; }
        }

        public String Address
        {
            get { return _position; }
            set { _position = value; }
        }

        public int Value
        {
            get { return _value; }
            set { _value = value; }
        }
        public String Units
        {
            get { return _unit; }
            set { _unit = value; }
        }

        public String Lenght
        {
            get { return _type; }
            set { _type = value; }
        }

        public String AdrVoltage
        {
            get { return _posvoltage; }
            set { _posvoltage = value; }
        }

        public int Voltage
        {
            get { return _voltage; }
            set { _voltage = value; }
        }
    }
}
