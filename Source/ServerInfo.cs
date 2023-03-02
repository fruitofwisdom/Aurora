using System;

namespace Aurora
{
    internal abstract class ServerInfoEventArgs : System.EventArgs { }

    internal class ServerInfoConnectionsArgs : ServerInfoEventArgs
    {
        public readonly int Connections;

        public ServerInfoConnectionsArgs(int connections)
        {
            Connections = connections;
        }
    }

    internal class ServerInfoGameArgs : ServerInfoEventArgs
    {
        public readonly bool Loaded;

        public ServerInfoGameArgs(bool loaded)
        {
            Loaded = loaded;
        }
    }

    internal class ServerInfoReportArgs : ServerInfoEventArgs
    {
        public readonly string Report;
        public readonly ColorCodes.Color Color = ColorCodes.Color.Reset;

        public ServerInfoReportArgs(string report)
        {
            Report = "<" + DateTime.Now + "> " + report;
        }

        public ServerInfoReportArgs(string report, ColorCodes.Color color)
        {
            Report = "<" + DateTime.Now + "> " + report;
            Color = color;
        }
    }

    internal class ServerInfoServerArgs : ServerInfoEventArgs
    {
        public readonly bool Running;

        public ServerInfoServerArgs(bool running)
        {
            Running = running;
        }
    }

    internal delegate void ServerInfoHandler(object sender, ServerInfoEventArgs args);

    internal class ServerInfo
    {
        public event ServerInfoHandler EventReceived;

        private static ServerInfo _instance = null;
        public static ServerInfo Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ServerInfo();
                }
                return _instance;
            }
        }

        private ServerInfo()
        {
            ;
        }

        public void RaiseEvent(ServerInfoEventArgs eventArgs)
        {
            // don't raise the event if no one is subscribed
            if (EventReceived != null)
            {
                EventReceived(this, eventArgs);
            }
        }

        public void Report(string report)
        {
            RaiseEvent(new ServerInfoReportArgs(report));
        }

        public void Report(ColorCodes.Color color, string report)
        {
            RaiseEvent(new ServerInfoReportArgs(report, color));
        }
    }
}
