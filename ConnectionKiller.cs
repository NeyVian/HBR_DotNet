using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace ConnectionKiller
{
    public class Connection
    {
        public string localAddress;
        public string remoteAddress;
        public int localPort;
        public int remotePort;
        public DateTime creationDate;

        public static Connection MostRecent(Connection a, Connection b)
        {
            int res = DateTime.Compare(a.creationDate, b.creationDate);
            if (res < 0) return b;
            return a;
        }

        public Connection(string lAddr, string lPort, string rAddr, string rPort, string cDate)
        {
            localAddress = lAddr;
            localPort = int.Parse(lPort);
            remoteAddress = rAddr;
            remotePort = int.Parse(rPort);
            creationDate = DateTime.Parse(cDate);

        }
    }

    public class ConnectionManagement
    {
        // Taken from https://github.com/yromen/repository/tree/master/DNProcessKiller
        // It part from the Disconnecter class. 
        // In case of nested class use "+" like that [ConnectionKiller.Program+Disconnecter]::Connections()

        /// <summary> 
        /// Enumeration of the states 
        /// </summary> 
        public enum State
        {
            /// <summary> All </summary> 
            All = 0,
            /// <summary> Closed </summary> 
            Closed = 1,
            /// <summary> Listen </summary> 
            Listen = 2,
            /// <summary> Syn_Sent </summary> 
            Syn_Sent = 3,
            /// <summary> Syn_Rcvd </summary> 
            Syn_Rcvd = 4,
            /// <summary> Established </summary> 
            Established = 5,
            /// <summary> Fin_Wait1 </summary> 
            Fin_Wait1 = 6,
            /// <summary> Fin_Wait2 </summary> 
            Fin_Wait2 = 7,
            /// <summary> Close_Wait </summary> 
            Close_Wait = 8,
            /// <summary> Closing </summary> 
            Closing = 9,
            /// <summary> Last_Ack </summary> 
            Last_Ack = 10,
            /// <summary> Time_Wait </summary> 
            Time_Wait = 11,
            /// <summary> Delete_TCB </summary> 
            Delete_TCB = 12
        }

        /// <summary> 
        /// Connection info 
        /// </summary> 
        private struct MIB_TCPROW
        {
            public int dwState;
            public int dwLocalAddr;
            public int dwLocalPort;
            public int dwRemoteAddr;
            public int dwRemotePort;
        }

        //API to change status of connection 
        [DllImport("iphlpapi.dll")]
        //private static extern int SetTcpEntry(MIB_TCPROW tcprow); 
        private static extern int SetTcpEntry(IntPtr pTcprow);

        //Convert 16-bit value from network to host byte order 
        [DllImport("wsock32.dll")]
        private static extern int ntohs(int netshort);

        //Convert 16-bit value back again 
        [DllImport("wsock32.dll")]
        private static extern int htons(int netshort);

        /// <summary> 
        /// Close a connection by returning the connectionstring 
        /// </summary> 
        /// <param name="connectionstring"></param> 
        
        public static void CloseConnection(Connection conn)
        {
            CloseConnection(conn.localAddress, conn.localPort, conn.remoteAddress, conn.remotePort);
        }

        public static void CloseConnection(string localAddress, int localPort, string remoteAddress, int remotePort)
        {
            try
            { 
                //if (parts.Length != 4) throw new Exception("Invalid connectionstring - use the one provided by Connections.");
                string[] locaddr = localAddress.Split('.');
                string[] remaddr = remoteAddress.Split('.');

                //Fill structure with data 
                MIB_TCPROW row = new MIB_TCPROW();
                row.dwState = 12;
                byte[] bLocAddr = new byte[] { byte.Parse(locaddr[0]), byte.Parse(locaddr[1]), byte.Parse(locaddr[2]), byte.Parse(locaddr[3]) };
                byte[] bRemAddr = new byte[] { byte.Parse(remaddr[0]), byte.Parse(remaddr[1]), byte.Parse(remaddr[2]), byte.Parse(remaddr[3]) };
                row.dwLocalAddr = BitConverter.ToInt32(bLocAddr, 0);
                row.dwRemoteAddr = BitConverter.ToInt32(bRemAddr, 0);
                row.dwLocalPort = htons(localPort);
                row.dwRemotePort = htons(remotePort);

                //Make copy of the structure into memory and use the pointer to call SetTcpEntry 
                IntPtr ptr = GetPtrToNewObject(row);
                int ret = SetTcpEntry(ptr);

                if (ret == -1) throw new Exception("Unsuccessful");
                if (ret == 65) throw new Exception("User has no sufficient privilege to execute this API successfully");
                if (ret == 87) throw new Exception("Specified port is not in state to be closed down");
                if (ret == 317) throw new Exception("The function is unable to set the TCP entry since the application is running non-elevated");
                if (ret != 0) throw new Exception("Unknown error (" + ret + ")");

            }
            catch (Exception ex)
            {
                throw new Exception("CloseConnection failed (" + localAddress + ":" + localPort + "->" + remoteAddress + ":" + remotePort + ")! [" + ex.GetType().ToString() + "," + ex.Message + "]");
            }
        }

        private static IntPtr GetPtrToNewObject(object obj)
        {
            IntPtr ptr = Marshal.AllocCoTaskMem(Marshal.SizeOf(obj));
            Marshal.StructureToPtr(obj, ptr, false);
            return ptr;
        }

        public static Connection? SearchConnection(
            string process, string hs_ports)
        {
            Connection? connCandidate = null;

            Process cmd = new ();
            cmd.StartInfo.FileName = "Powershell.Exe";
            cmd.StartInfo.WorkingDirectory = "c:/windows/system32";
            cmd.StartInfo.Arguments = "Get-NetTCPConnection -State Established | Select-Object -Property OwningProcess, LocalAddress, LocalPort, RemoteADdress, RemotePort, CreationTime";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.RedirectStandardError = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;

            try
            {
                cmd.Start();
                StreamReader stdOutput = cmd.StandardOutput;
                StreamReader stdError = cmd.StandardError;
                var content = stdOutput.ReadToEnd() + stdError.ReadToEnd();
                var exitStatus = cmd.ExitCode.ToString();

                content = Regex.Replace(content, "\r\n\r\n", "|").Trim();
                foreach (string row in content.Split("|"))
                {
                    var fields = row.Split("\r\n");
                    if (fields is not null && fields.Length.Equals(6))
                    {
                        var sep = ": ";
                        int pid = 0;
                        if (!int.TryParse(fields[0].Split(sep)[1], out pid)) continue;
                        var pname = (Process.GetProcessById(pid)).ProcessName;

                        if (!process.Equals(pname, StringComparison.OrdinalIgnoreCase)) continue;

                        var laddr = fields[1].Split(sep)[1];
                        var lport = fields[2].Split(sep)[1];
                        var raddr = fields[3].Split(sep)[1];
                        var rport = fields[4].Split(sep)[1];
                        var cdate = fields[5].Split(sep)[1];

                        if (!hs_ports.Contains(rport)) continue;

                        var c = new Connection(laddr, lport, raddr, rport, cdate);

                        if (connCandidate is not null)
                        {
                            connCandidate = Connection.MostRecent(connCandidate, c);
                        }
                        else
                        {
                            connCandidate = c;
                        }

                    }
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return connCandidate;
        }

    }
}