using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WPF_XML_Tutorial
{
    class INIFile
    {
        string filePath;
        string exe = Assembly.GetExecutingAssembly ().GetName ().Name;

        [DllImport ( "kernel32", CharSet = CharSet.Unicode )]
        static extern long WritePrivateProfileString( string Section, string Key, string Value, string FilePath );

        [DllImport ( "kernel32", CharSet = CharSet.Unicode )]
        static extern int GetPrivateProfileString( string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath );


        public INIFile( string IniPath = null )
        {
            filePath = new FileInfo ( IniPath ?? exe + ".ini" ).FullName.ToString ();
        }

        public string Read( string Key, string Section = null )
        {
            var RetVal = new StringBuilder ( 255 );
            GetPrivateProfileString ( Section ?? exe, Key, "", RetVal, 255, filePath );
            return RetVal.ToString ();
        }

        public void Write( string Key, string Value, string Section = null )
        {
            WritePrivateProfileString ( Section ?? exe, Key, Value, filePath );
        }

        public void DeleteKey( string Key, string Section = null )
        {
            Write ( Key, null, Section ?? exe );
        }

        public void DeleteSection( string Section = null )
        {
            Write ( null, null, Section ?? exe );
        }

        public bool KeyExists( string Key, string Section = null )
        {
            return Read ( Key, Section ).Length > 0;
        }
    }
}
