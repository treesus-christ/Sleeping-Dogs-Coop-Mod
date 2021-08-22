using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Memory;

namespace Sleeping_Dogs_Mods
{
    public partial class Form1 : Form
    {
        class MemoryAPI
        {
            [Flags]
            public enum AllocType
            {
                Commit = 0x1000,
                Reserve = 0x2000,
                Decommit = 0x4000,
            }
            [Flags]
            public enum Protect
            {
                Execute = 0x10,
                ExecuteRead = 0x20,
                ExecuteReadWrite = 0x40,
            }
            [Flags]
            public enum FreeType
            {
                Decommit = 0x4000,
                Release = 0x8000,
            }
            [Flags]
            public enum ProcessAccessType
            {
                PROCESS_TERMINATE = (0x0001),
                PROCESS_CREATE_THREAD = (0x0002),
                PROCESS_SET_SESSIONID = (0x0004),
                PROCESS_VM_OPERATION = (0x0008),
                PROCESS_VM_READ = (0x0010),
                PROCESS_VM_WRITE = (0x0020),
                PROCESS_DUP_HANDLE = (0x0040),
                PROCESS_CREATE_PROCESS = (0x0080),
                PROCESS_SET_QUOTA = (0x0100),
                PROCESS_SET_INFORMATION = (0x0200),
                PROCESS_QUERY_INFORMATION = (0x0400)
            }

            [DllImport("kernel32.dll")]
            public static extern IntPtr OpenProcess(UInt64 dwDesiredAccess, Int64 bInheritHandle, UInt64 dwProcessId);

            [DllImport("kernel32.dll")]
            public static extern Int64 CloseHandle(IntPtr hObject);

            [DllImport("kernel32.dll")]
            public static extern Int64 ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [In, Out] byte[] buffer, UInt64 size, out IntPtr lpNumberOfBytesRead);

            [DllImport("kernel32.dll")]
            public static extern Int64 WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [In, Out] byte[] buffer, UInt64 size, out IntPtr lpNumberOfBytesWritten);
            [DllImport("kernel32.dll")]
            public static extern Int64 VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, uint flAllocationType, Protect flProtect);
            [DllImport("kernel32.dll")]
            public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, FreeType dwFreeType);
        }

        public class Memory
        {
            public Memory()
            {
            }

            public Process ReadProcess
            {
                get
                {
                    return m_ReadProcess;
                }
                set
                {
                    m_ReadProcess = value;
                }
            }
            private Process m_ReadProcess = null;
            private IntPtr m_hProcess = IntPtr.Zero;

            public void Open()
            {
                MemoryAPI.ProcessAccessType access = MemoryAPI.ProcessAccessType.PROCESS_VM_READ
                | MemoryAPI.ProcessAccessType.PROCESS_VM_WRITE
                | MemoryAPI.ProcessAccessType.PROCESS_VM_OPERATION;
                m_hProcess = MemoryAPI.OpenProcess((uint)access, 1, (uint)m_ReadProcess.Id);
            }

            public void CloseHandle()
            {
                long iRetValue;
                iRetValue = MemoryAPI.CloseHandle(m_hProcess);
                if (iRetValue == 0)
                    throw new Exception("CloseHandle Failed");
            }

            public byte[] Read(IntPtr MemoryAddress, uint bytesToRead, out long bytesRead)
            {
                byte[] buffer = new byte[bytesToRead];
                IntPtr ptrBytesRead;
                MemoryAPI.ReadProcessMemory(m_hProcess, MemoryAddress, buffer, bytesToRead, out ptrBytesRead);
                bytesRead = ptrBytesRead.ToInt64();
                return buffer;
            }

            public byte[] PointerRead(IntPtr MemoryAddress, uint bytesToRead, uint[] Offset, out long bytesRead)
            {
                int iPointerCount = Offset.Length - 1;
                IntPtr ptrBytesRead;
                bytesRead = 0;
                byte[] buffer = new byte[4]; //DWORD to hold an Address
                long tempAddress = 0;

                if (iPointerCount == 0)
                {
                    MemoryAPI.ReadProcessMemory(m_hProcess, MemoryAddress, buffer, 4, out ptrBytesRead);
                    tempAddress = Addr.ToDec(Addr.Make(buffer)) + Offset[0]; //Final Address

                    buffer = new byte[bytesToRead];
                    MemoryAPI.ReadProcessMemory(m_hProcess, (IntPtr)tempAddress, buffer, bytesToRead, out ptrBytesRead);

                    bytesRead = ptrBytesRead.ToInt64();
                    return buffer;
                }

                for (int i = 0; i <= iPointerCount; i++)
                {
                    if (i == iPointerCount)
                    {
                        MemoryAPI.ReadProcessMemory(m_hProcess, (IntPtr)tempAddress, buffer, 4, out ptrBytesRead);
                        tempAddress = Addr.ToDec(Addr.Make(buffer)) + Offset[i]; //Final Address

                        buffer = new byte[bytesToRead];
                        MemoryAPI.ReadProcessMemory(m_hProcess, (IntPtr)tempAddress, buffer, bytesToRead, out ptrBytesRead);

                        bytesRead = ptrBytesRead.ToInt64();
                        return buffer;
                    }
                    else if (i == 0)
                    {
                        MemoryAPI.ReadProcessMemory(m_hProcess, MemoryAddress, buffer, 4, out ptrBytesRead);
                        tempAddress = Addr.ToDec(Addr.Make(buffer)) + Offset[1];
                    }
                    else
                    {
                        MemoryAPI.ReadProcessMemory(m_hProcess, (IntPtr)tempAddress, buffer, 4, out ptrBytesRead);
                        tempAddress = Addr.ToDec(Addr.Make(buffer)) + Offset[i];
                    }
                }

                return buffer;
            }

            public void Write(IntPtr MemoryAddress, byte[] bytesToWrite, out long bytesWritten)
            {
                IntPtr ptrBytesWritten;
                MemoryAPI.WriteProcessMemory(m_hProcess, MemoryAddress, bytesToWrite, (ulong)bytesToWrite.Length, out ptrBytesWritten);
                bytesWritten = ptrBytesWritten.ToInt64();
            }

            public string PointerWrite(IntPtr MemoryAddress, byte[] bytesToWrite, uint[] Offset, out long bytesWritten)
            {
                uint iPointerCount = (uint)Offset.Length - 1;
                IntPtr ptrBytesWritten;
                bytesWritten = 0;
                byte[] buffer = new byte[4]; //DWORD to hold an Address
                long tempAddress = 0;

                if (iPointerCount == 0)
                {
                    MemoryAPI.ReadProcessMemory(m_hProcess, MemoryAddress, buffer, 4, out ptrBytesWritten);
                    tempAddress = Addr.ToDec(Addr.Make(buffer)) + Offset[0]; //Final Address
                    MemoryAPI.WriteProcessMemory(m_hProcess, (IntPtr)tempAddress, bytesToWrite, (uint)bytesToWrite.Length, out ptrBytesWritten);

                    bytesWritten = ptrBytesWritten.ToInt64();
                    return Addr.ToHex(tempAddress);
                }

                for (int i = 0; i <= iPointerCount; i++)
                {
                    if (i == iPointerCount)
                    {
                        MemoryAPI.ReadProcessMemory(m_hProcess, (IntPtr)tempAddress, buffer, 4, out ptrBytesWritten);
                        tempAddress = Addr.ToDec(Addr.Make(buffer)) + Offset[i]; //Final Address
                        MemoryAPI.WriteProcessMemory(m_hProcess, (IntPtr)tempAddress, bytesToWrite, (uint)bytesToWrite.Length, out ptrBytesWritten);

                        bytesWritten = ptrBytesWritten.ToInt64();
                        return Addr.ToHex(tempAddress);
                    }
                    else if (i == 0)
                    {
                        MemoryAPI.ReadProcessMemory(m_hProcess, MemoryAddress, buffer, 4, out ptrBytesWritten);
                        tempAddress = Addr.ToDec(Addr.Make(buffer)) + Offset[i];
                    }
                    else
                    {
                        MemoryAPI.ReadProcessMemory(m_hProcess, (IntPtr)tempAddress, buffer, 4, out ptrBytesWritten);
                        tempAddress = Addr.ToDec(Addr.Make(buffer)) + Offset[i];
                    }
                }

                return Addr.ToHex(tempAddress);
            }

            public int PID()
            {
                return m_ReadProcess.Id;
            }

            public string BaseAddressH()
            {
                return Addr.ToHex((uint)(IntPtr)m_ReadProcess.MainModule.BaseAddress.ToInt64());
            }

            public long BaseAddressD()
            {
                return m_ReadProcess.MainModule.BaseAddress.ToInt64();
            }

            public void Alloc(out long Addr, int Size)
            {
                Addr = MemoryAPI.VirtualAllocEx(m_hProcess, IntPtr.Zero, Size, (uint)MemoryAPI.AllocType.Commit | (uint)MemoryAPI.AllocType.Reserve, MemoryAPI.Protect.ExecuteReadWrite);
            }
            public void Alloc(out long Addr, long startingAddress, int Size)
            {
                Addr = MemoryAPI.VirtualAllocEx(m_hProcess, (IntPtr)startingAddress, Size, (uint)MemoryAPI.AllocType.Commit | (uint)MemoryAPI.AllocType.Reserve, MemoryAPI.Protect.ExecuteReadWrite);
            }
            public bool Dealloc(long Addr)
            {
                return MemoryAPI.VirtualFreeEx(m_hProcess, (IntPtr)Addr, 0, MemoryAPI.FreeType.Release);
            }
        }

        public class Addr
        {
            public static string Make(byte[] buffer)
            {
                string sTemp = "";

                for (int i = 0; i < buffer.Length; i++)
                {
                    if (Convert.ToInt16(buffer[i]) < 10)
                        sTemp = "0" + ToHex(buffer[i]) + sTemp;
                    else
                        sTemp = ToHex(buffer[i]) + sTemp;
                }

                return sTemp;
            }

            public static string ToHex(long Decimal)
            {
                return Decimal.ToString("X"); //Convert Decimal to Hexadecimal
            }

            public static long ToDec(string Hex)
            {
                return long.Parse(Hex, NumberStyles.HexNumber); //Convert Hexadecimal to Decimal
            }
        }

        public class MemoryContext
        {
            const int PROCESS_WM_READ = 0x0010;

            [DllImport("kernel32.dll")]
            public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

            [DllImport("kernel32.dll")]
            public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

            private long baseAddress;
            private Process _process;
            private IntPtr handle;

            public MemoryContext(Process process)
            {
                _process = process;
                baseAddress = _process.MainModule.BaseAddress.ToInt64();
                handle = OpenProcess(PROCESS_WM_READ, false, _process.Id);
            }

            public string GetBaseAddress()
            {
                return string.Format("{0:X}", baseAddress);
            }

            public long GetBaseAddressLong()
            {
                return baseAddress;
            }

            public string AddHexOffsetToBaseAddress(string hex)
            {
                long newAddress = baseAddress + long.Parse(hex, NumberStyles.HexNumber);
                return string.Format("{0:X}", newAddress);
            }

            public long AddHexOffsetToBaseAddressLong(string hex)
            {
                long newAddress = baseAddress + long.Parse(hex, NumberStyles.HexNumber);
                return newAddress;
            }
        }

        Mem connection = new Mem();
        Memory oMemory = new Memory();
        MemoryContext memcon;


        //"SDHDShip.exe"+02409CE0 + 3C4
        string unlimited_money_address = "SDHDShip.exe+0x02409CE0,0x3C4";

        //
        string unlimited_health_address = "SDHDShip.exe+0x02087B78,0x14";

        string x_position_address = "SDHDShip.exe+0x021738A8,0x220";
        string y_position_address = "SDHDShip.exe+0x021738A8,0x228";
        string z_position_address = "SDHDShip.exe+0x021738A8,0x224";

        bool EnableHackAllocCheck = false;

        public Form1()
        {
            InitializeComponent();

            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            int PID = connection.GetProcIdFromName("sdhdship");
            if (PID > 0)
            {
                connection.OpenProcess(PID);
                textBox1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(CheckEnterKeyPress1);
                textBox2.KeyPress += new System.Windows.Forms.KeyPressEventHandler(CheckEnterKeyPress2);
                textBox3.KeyPress += new System.Windows.Forms.KeyPressEventHandler(CheckEnterKeyPress3);
                memcon = new MemoryContext(Process.GetProcessById(PID));

                if (!backgroundWorker1.IsBusy)
                {
                    backgroundWorker1.RunWorkerAsync();
                }
            }
        }

        long newmem = 0;

        private void EnableHack()
        {
            if (EnableHackAllocCheck) return;
            Process[] process = Process.GetProcessesByName("sdhdship"); //Find FarmFrenzy3_Arctica.wrp.exe
            oMemory.ReadProcess = process[0]; //Sets the Process to Read/Write From/To
            oMemory.Open(); //Open Process
            memcon = new MemoryContext(process[0]);
            //if (!EnableHackAllocCheck)
            //{
                //oMemory.Alloc(out newmem, 1000); //New memory allocation "code cave"
                oMemory.Alloc(out newmem, memcon.GetBaseAddressLong() - 0x10000, 4096); //New memory allocation "code cave"
                //while (string.Format("{0:X}", newmem).Length < 12)
                //{
                //    oMemory.Alloc(out newmem, long.Parse("100000000000", NumberStyles.HexNumber), 4096); //New memory allocation "code cave"
                //    if (string.Format("{0:X}", newmem).Length >= 12)
                //    {
                //        break;
                //    }
                //    else
                //    {
                //        oMemory.Dealloc(newmem); //Release newmem "code cave"
                //    }
                //}
            //}

            //byte[] originalBytes = { 0xF3, 0x0F, 0x11, 0x40, 0x04 };
            //byte[] newBytes = { 0xE9, 0x7A, 0xC2 };
            //UIntPtr codecavebase = connection.CreateCodeCave("base+63D81", newBytes, 6, 1000);
            //UIntPtr codecaveAllocAddress = UIntPtr.Add(codecavebase, newBytes.Length);
            long ad1 = Addr.ToDec(memcon.AddHexOffsetToBaseAddress("54F961")); //Static address
            string ad1_HexAddress = string.Format("{0:X}", ad1);
            long ad2 = newmem; //newmem
            string ad2_HexAddress = string.Format("{0:X}", ad2);
            long ad3 = ad2 + 0x0B; //newmem + 6 bytes (see bv2 below)
            string ad3_HexAddress = string.Format("{0:X}", ad3);
            long ad4 = ad2 + 0x12; //newmem + 10 bytes (see bv3 below)
            string ad4_HexAddress = string.Format("{0:X}", ad4);
            long ad5 = ad1 + 0x07;
            string ad5_HexAddress = string.Format("{0:X}", ad5);

            byte[] bv1 = Jmp(Addr.ToHex(ad2), Addr.ToHex(ad1), true); //jmp newmem nop (jmp newmem = 5 bytes, so add a nop)
            string[] bv1_Hex = new string[bv1.Length];
            for (int i = 0; i < bv1.Length; i++)
            {
                bv1_Hex[i] = string.Format("{0:X}", bv1[i]);
            }
            byte[] bv2 = { 0xC7, 0x84, 0x83, 0xD4, 0x00, 0x00, 0x00, 0x99, 0x09, 0x00, 0x00 }; //7FF6B6DC0000 - mov [rbx+rax*4+000000D4],00000999
            byte[] bv3 = { 0x8B, 0x94, 0x83, 0xD4, 0x00, 0x00, 0x00 }; //7FF7654A0012 - mov edx,[rbx+rax*4+000000D4]
            byte[] bv4 = Jmp(Addr.ToHex(ad5), Addr.ToHex(ad4), false); //jmp 00545949 (ad1 + 7 bytes)
            string[] bv4_Hex = new string[bv4.Length];
            for (int i = 0; i < bv4.Length; i++)
            {
                bv4_Hex[i] = string.Format("{0:X}", bv4[i]);
            }

            long bytes; //Holds how many bytes were written by Write()

            oMemory.Write((IntPtr)ad1, bv1, out bytes);
            oMemory.Write((IntPtr)ad2, bv2, out bytes);
            oMemory.Write((IntPtr)ad3, bv3, out bytes);
            oMemory.Write((IntPtr)ad4, bv4, out bytes);

            oMemory.CloseHandle(); //Close Memory Handle
            EnableHackAllocCheck = true;
        }

        private void DisableHack()
        {
            if (!EnableHackAllocCheck) return;
            Process[] process = Process.GetProcessesByName("sdhdship"); //Find FarmFrenzy3_Arctica.wrp.exe
            oMemory.ReadProcess = process[0]; //Sets the Process to Read/Write From/To
            oMemory.Open(); //Open Process
            oMemory.Dealloc(newmem); //Release newmem "code cave"

            //SDHDShip.exe+54F961
            long ad1 = Addr.ToDec(memcon.AddHexOffsetToBaseAddress("54F961")); //Static address
            byte[] bv1 = { 0x8B, 0x94, 0x83, 0xD4, 0x00, 0x00, 0x00 }; //mov edx,[rbx+rax*4+000000D4]

            long bytes; //Holds how many bytes were written by Write()

            oMemory.Write((IntPtr)ad1, bv1, out bytes);

            oMemory.CloseHandle(); //Close Memory Handle
            EnableHackAllocCheck = false;
        }

        private byte[] Jmp(string to, string from, bool nop)
        {
            return Jmp(Convert.ToInt64("0x" + to, 16), Convert.ToInt64("0x" + from, 16), nop);
        }

        private byte[] Jmp(long to, long from, bool nop)
        {
            string dump;
            //string dump = (to - from - 5).ToString("X"); //Get original bytes
            //dump = (0 - (from - to) - 5).ToString("X"); //Get original bytes
            dump = ((int)(0 - (from - to) - 5)).ToString("X"); //Get original bytes

            //dump = dump.Substring(dump.Length / 2, dump.Length / 2);

            if (dump.Length % 4 != 0) //Make sure we have 4 bytes
            {
                while (dump.Length % 4 != 0)
                {
                    dump = "0" + dump;
                    //dump = dump + "0";
                }
            }
            dump = dump + "E9"; //Add JMP
            if (nop)
                dump = "9066" + dump; //Add NOP if needed

            byte[] hex = new byte[dump.Length / 2];
            for (int i = 0; i < hex.Length; i++)
            {
                hex[i] = Convert.ToByte(dump.Substring(i * 2, 2), 16); //Set each byte to 2 chars
            }
            Array.Reverse(hex); //Reverse byte array for use with Write()

            return hex;
        }

        private void CheckEnterKeyPress1(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                connection.FreezeValue(x_position_address, "float",textBox1.Text);
            }
        }

        private void CheckEnterKeyPress2(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                connection.FreezeValue(y_position_address, "float", textBox2.Text);
            }
        }

        private void CheckEnterKeyPress3(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                connection.FreezeValue(z_position_address, "float", textBox3.Text);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            connection.UnfreezeValue(unlimited_money_address);
            connection.UnfreezeValue(unlimited_health_address);
            connection.UnfreezeValue(x_position_address);
            connection.UnfreezeValue(y_position_address);
            connection.UnfreezeValue(z_position_address);
            connection.UnfreezeValue(z_position_address);
            DisableHack();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                if (checkBox1.Checked)
                {
                    connection.FreezeValue(unlimited_money_address, "int", "10000000");
                }

                if (checkBox2.Checked)
                {
                    connection.FreezeValue(unlimited_health_address, "float", "2000");
                }

                //X
                if (checkBox3.Checked)
                {
                    textBox1.ReadOnly = false;
                }
                else
                {
                    textBox1.Text = connection.ReadFloat(x_position_address).ToString();
                    textBox1.ReadOnly = true;
                }

                //Y
                if (checkBox4.Checked)
                {
                    textBox2.ReadOnly = false;
                }
                else
                {
                    textBox2.Text = connection.ReadFloat(y_position_address).ToString();
                    textBox2.ReadOnly = true;
                }

                //Z
                if (checkBox5.Checked)
                {
                    textBox3.ReadOnly = false;
                }
                else
                {
                    textBox3.Text = connection.ReadFloat(z_position_address).ToString();
                    textBox3.ReadOnly = true;
                }

                if (checkBox6.Checked)
                {
                    EnableHack();
                }
                else
                {
                    DisableHack();
                }
                connection.UnfreezeValue(unlimited_money_address);
                connection.UnfreezeValue(unlimited_health_address);
                connection.UnfreezeValue(x_position_address);
                connection.UnfreezeValue(y_position_address);
                connection.UnfreezeValue(z_position_address);
                connection.UnfreezeValue(z_position_address);
            }
        }
    }
}
