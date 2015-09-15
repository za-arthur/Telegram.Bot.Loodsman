using DataProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.Bot.Loodsman
{
    class Loodsman : IDisposable
    {
        private LoodsmanConnectionClass lConnection;
        private LoodsmanAPI8 lAPI8;

        public void Connect(string userName, string userPass)
        {
            lConnection = new LoodsmanConnectionClass();
            lConnection.SetConnectionString(Properties.Settings.Default.AppServer);
            lConnection.Connected = true;

            lAPI8 = lConnection.API8;
            lAPI8.RunMethod("ConnectToDBEx", new object[] { Properties.Settings.Default.DBName, userName, userPass });
        }

        public void Disconnect()
        {
            if (lConnection == null)
                return;

            lConnection.Connected = false;
            Marshal.ReleaseComObject(lConnection);
            lConnection = null;
            Marshal.ReleaseComObject(lAPI8);
            lAPI8 = null;
        }

        public void Dispose()
        {
            Disconnect();
            GC.SuppressFinalize(this);
        }

        ~Loodsman()
        {
            Disconnect();
        }

        public LoodsmanObject ProjectList()
        {
            var rootObject = new LoodsmanObject();
            var projectsData = lAPI8.GetDataSet("GetProjectListEx", new object[] { false }) as IDataSet;

            while (!projectsData.Eof)
            {
                rootObject.Childs.Add(new LoodsmanObject()
                    {
                        Id = projectsData.ValueAsInt("_ID_VERSION"),
                        Product = projectsData.ValueAsString("_PRODUCT")
                    });
                projectsData.Next();
            }

            return rootObject;
        }

        public LoodsmanObject ChildList(LoodsmanObject parentObject)
        {
            if (parentObject == null || parentObject.Id == 0)
                return ProjectList();
            else
            {
                var objectsData = lAPI8.GetDataSet("GetTree", new object[] { "", "", "", parentObject.Id,
                    "Состоит из ..." + (Char)1 + "Документы", false }) as IDataSet;
                parentObject.Childs.Clear();
                while (!objectsData.Eof)
                {
                    parentObject.Childs.Add(new LoodsmanObject()
                        {
                            Id = objectsData.ValueAsInt("_ID_VERSION"),
                            Product = objectsData.ValueAsString("_PRODUCT")
                        });
                    objectsData.Next();
                }
            }
            return parentObject;
        }

        public string[] Attributes(LoodsmanObject obj)
        {
            var attrsData = lAPI8.GetDataSet("GetInfoAboutVersion", new object[] { "", "", "",
                obj.Id, 2 }) as IDataSet;
            var attrs = new string[attrsData.RecordCount];

            var i = 0;
            while (!attrsData.Eof)
            {
                attrs[i] = attrsData.ValueAsString("_NAME") + ": " + attrsData.ValueAsString("_VALUE");
                i++;
                attrsData.Next();
            }

            return attrs;
        }

        public void FileList(LoodsmanObject obj)
        {
            var filesData = lAPI8.GetDataSet("GetInfoAboutVersion", new object[] { "", "", "", obj.Id, 7 }) as IDataSet;
            obj.Files.Clear();
            while (!filesData.Eof)
            {
                obj.Files.Add(new LoodsmanFile()
                    {
                        Name = filesData.ValueAsString("_NAME"),
                        LocalName = filesData.ValueAsString("_LOCALNAME")
                    });
                filesData.Next();
            }
        }

        public string GetFile(LoodsmanObject obj, LoodsmanFile file)
        {
            return (lAPI8.RunMethod("GetFileById", new object[] { obj.Id, file.Name, file.LocalName }) ?? "").ToString();
        }
    }
}
