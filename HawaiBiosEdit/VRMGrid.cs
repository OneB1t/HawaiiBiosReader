using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HawaiiBiosReader
{
    public class VRMGrid
    {
        private String _position;
        private int _value;
        private String _unit;
        private String _type;


        public VRMGrid(String pos, int val, String un, String type)
        {
            _position = pos;
            _value = val;
            _unit = un;
            _type = type;
        }
        public String Info
        {
            get { return _unit; }
            set { _unit = value; }
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
        public String Length
        {
            get { return _type; }
            set { _type = value; }
        }
    }
}
