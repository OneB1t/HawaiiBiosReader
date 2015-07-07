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

        public GridRow(String pos, int val, String un, String type, int dpm)
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
}
