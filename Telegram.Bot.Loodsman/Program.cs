using DataProvider;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Telegram.Bot.Loodsman
{
    class Program
    {
        private static Dictionary<int, UserSession> userSessions = new Dictionary<int, UserSession>();

        static void Main(string[] args)
        {
            Run().Wait();
        }

        private static UserSession GetUserSession(int userId)
        {
            if (userSessions.ContainsKey(userId))
                return userSessions[userId];
            else
            {
                var session = new UserSession()
                {
                    UserId = userId
                };
                userSessions.Add(userId, session);
                return session;
            }
        }

        private static bool CheckConnection(Api bot, UserSession session)
        {
            if (session.Connection == null)
                bot.SendTextMessage(session.UserId, "Выполните подключение к базе данных");
            else
                return true;
            return false;
        }

        private static void SetTask(UserSession session, Action action)
        {
            var task = session.CurrentTask;
            if (task == null)
                session.CurrentTask = Task.Run(action);
            else
                task.ContinueWith(_ => action());
        }

        private static void ConnectTask(Api bot, UserSession session, string userName, string userPass)
        {
            if (session.Connection != null)
                session.Connection.Disconnect();
            else
                session.Connection = new Loodsman();
            try
            {
                session.Connection.Connect(userName, userPass);
                bot.SendTextMessage(session.UserId, "Подключение выполнено успешно");
            }
            catch (COMException)
            {
                bot.SendTextMessage(session.UserId, "Ошибка соединения с базой данных");
            }
        }

        private static void DisconnectTask(Api bot, UserSession session)
        {
            if (CheckConnection(bot, session))
            {
                session.Connection.Disconnect();
                session.Connection = null;
                if (userSessions.ContainsKey(session.UserId))
                    userSessions.Remove(session.UserId);
                bot.SendTextMessage(session.UserId, "Отключение соединения выполнено успешно");
            }
        }

        private static void ProjectsTask(Api bot, UserSession session)
        {
            if (CheckConnection(bot, session))
            {
                try
                {
                    var rootObj = session.Connection.ProjectList();
                    bot.SendTextMessage(session.UserId, "Список проектов:" + Environment.NewLine
                        + rootObj.Childs.ToStringEx());
                    session.CurrentObject = rootObj;
                }
                catch (COMException)
                {
                    bot.SendTextMessage(session.UserId, "Ошибка при получении списка проектов");
                }
            }
        }

        private static void ObjectTask(Api bot, UserSession session)
        {
            if (CheckConnection(bot, session))
            {
                try
                {
                    LoodsmanObject curObj = session.Connection.ChildList(session.CurrentObject);
                    
                    if (curObj.Childs.Count == 0)
                        bot.SendTextMessage(session.UserId, "Не найдено объектов");
                    else
                        bot.SendTextMessage(session.UserId, "Выберите объект", false, 0,
                                    new ReplyKeyboardMarkup()
                                    {
                                        Keyboard = curObj.Childs.ToKeyboard(),
                                        ResizeKeyboard = true,
                                        OneTimeKeyboard = true
                                    });
                    session.CurrentObject = curObj;
                }
                catch (COMException)
                {
                    bot.SendTextMessage(session.UserId, "Ошибка при получении списка объектов");
                }
            }
        }

        private static void ObjectTask(Api bot, UserSession session, string objProduct)
        {
            if (CheckConnection(bot, session) && session.CurrentObject != null)
            {
                try
                {
                    var obj = session.CurrentObject.Childs.Find(o => o.Product.Equals(objProduct, StringComparison.InvariantCultureIgnoreCase));
                    if (obj == null)
                        bot.SendTextMessage(session.UserId, "Объект не найден");
                    else
                    {
                        session.CurrentObject = obj;

                        var attrs = session.Connection.Attributes(obj);
                        if (attrs.Count() > 0)
                            bot.SendTextMessage(session.UserId, "Атрибуты объекта:" + Environment.NewLine
                                + string.Join(Environment.NewLine, attrs), false, 0, new ReplyKeyboardHide() { HideKeyboard = true });

                        session.Connection.FileList(obj);
                        if (obj.Files.Count > 0)
                            bot.SendTextMessage(session.UserId, "Файлы объекта:" + Environment.NewLine
                                + obj.Files.ToStringEx(), false, 0, new ReplyKeyboardHide() { HideKeyboard = true });

                        if (attrs.Count() == 0 && obj.Files.Count == 0)
                            bot.SendTextMessage(session.UserId, "У объекта нет атрибутов и файлов",
                                false, 0, new ReplyKeyboardHide() { HideKeyboard = true });
                    }
                }
                catch (COMException)
                {
                    bot.SendTextMessage(session.UserId, "Ошибка при получении информации об объекте",
                        false, 0, new ReplyKeyboardHide() { HideKeyboard = true });
                }
            }
        }

        private static void FileTask(Api bot, UserSession session)
        {
            if (CheckConnection(bot, session))
            {
                if (session.CurrentObject == null)
                    bot.SendTextMessage(session.UserId, "Не выбран объект");
                else if (session.CurrentObject.Files.Count == 0)
                    bot.SendTextMessage(session.UserId, "У объекта нет файлов");
                else
                {
                    bot.SendTextMessage(session.UserId, "Выберите файл", false, 0,
                                new ReplyKeyboardMarkup()
                                {
                                    Keyboard = session.CurrentObject.Files.ToKeyboard(),
                                    ResizeKeyboard = true,
                                    OneTimeKeyboard = true
                                });
                }
            }
        }

        private static void FileTask(Api bot, UserSession session, string fileName)
        {
            if (CheckConnection(bot, session))
            {
                if (session.CurrentObject == null)
                    bot.SendTextMessage(session.UserId, "Не выбран объект");
                else
                {
                    var file = session.CurrentObject.Files.Find(f => f.Name.Equals(fileName, StringComparison.InvariantCultureIgnoreCase));
                    if (file == null)
                        bot.SendTextMessage(session.UserId, "Файл не найден");
                    else
                        try
                        {
                            var fileFullName = session.Connection.GetFile(session.CurrentObject, file);
                            var ext = Path.GetExtension(fileName);
                            using (FileStream fs = new FileStream(fileFullName, FileMode.Open, FileAccess.Read))
                            {
                                Task<Message> task;
                                // Отправим фото
                                if (new[] { ".jpg", ".jpeg", ".gif", ".tif", ".bmp" }.Contains(ext))
                                    task = bot.SendPhoto(session.UserId, new FileToSend(fileName, fs),
                                        fileName, 0, new ReplyKeyboardHide() { HideKeyboard = true });
                                else
                                    task = bot.SendDocument(session.UserId, new FileToSend(fileName, fs),
                                        0, new ReplyKeyboardHide() { HideKeyboard = true });
                                task.Wait();
                            }
                        }
                        catch (COMException)
                        {
                            bot.SendTextMessage(session.UserId, "Ошибка при получении файла",
                                false, 0, new ReplyKeyboardHide() { HideKeyboard = true });
                        }
                }
            }
        }
        
        private static async Task Run()
        {
            if (!String.IsNullOrEmpty(Properties.Settings.Default.ProxyUser))
            {
                var proxyCreds = new NetworkCredential(
                    Properties.Settings.Default.ProxyUser,
                    Properties.Settings.Default.ProxyPassword
                );

                WebRequest.DefaultWebProxy.Credentials = proxyCreds;
            }

            var bot = new Api(Properties.Settings.Default.BotToken);
            var me = await bot.GetMe();

            Console.WriteLine("Hello my name is {0}", me.FirstName);

            var offset = 0;

            while (true)
            {
                var updates = await bot.GetUpdates(offset);

                foreach (var update in updates)
                {
                    if (update.Message.Text != null
                        && update.Message.From.Id > 0
                        // Только приватные чаты
                        && update.Message.From.Id == update.Message.Chat.Id)
                    {
                        var session = GetUserSession(update.Message.From.Id);
                        if (update.Message.Text == "/connect")
                        {
                            session.LastCommand = update.Message.Text;
                            await bot.SendTextMessage(session.UserId, "Введите имя пользователя и его пароль в две строки");
                        }
                        else if (update.Message.Text == "/disconnect")
                        {
                            SetTask(session, () => DisconnectTask(bot, session));
                            session.LastCommand = "";
                        }
                        else if (update.Message.Text == "/projects")
                        {
                            SetTask(session, () => ProjectsTask(bot, session));
                            session.LastCommand = "";
                        }
                        else if (update.Message.Text == "/object")
                        {
                            SetTask(session, () => ObjectTask(bot, session));                            
                            session.LastCommand = update.Message.Text;
                        }
                        else if (update.Message.Text == "/file")
                        {
                            SetTask(session, () => FileTask(bot, session));
                            session.LastCommand = update.Message.Text;
                        }
                        else if (update.Message.Text == "/subscribe")
                        {
                            session.LastCommand = "";
                        }
                        else if (update.Message.Text == "/start" || update.Message.Text == "/help")
                        {
                            session.LastCommand = "";
                        }
                        else if (session.LastCommand == "/connect")
                        {
                            var cred = update.Message.Text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                            if (cred.Count() != 2)
                                await bot.SendTextMessage(session.UserId, "Введите имя пользователя и его пароль в две строки");
                            else
                            {
                                SetTask(session, () => ConnectTask(bot, session, cred[0], cred[1]));
                                session.LastCommand = "";
                            }
                        }
                        else if (session.LastCommand == "/object")
                        {
                            SetTask(session, () => ObjectTask(bot, session, update.Message.Text));
                            session.LastCommand = "";
                        }
                        else if (session.LastCommand == "/file")
                        {
                            SetTask(session, () => FileTask(bot, session, update.Message.Text));
                            session.LastCommand = "";
                        }
                        else
                        {
                            session.LastCommand = "";
                            await bot.SendTextMessage(session.UserId, "Неизвестная команда");
                        }
                    }

                    offset = update.Id + 1;
                }

                await Task.Delay(1000);
            }
        }
    }
}