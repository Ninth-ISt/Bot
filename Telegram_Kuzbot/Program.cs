using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace Telegram_Kuzbot
{
    public class Program
    {
        //private static string Token { get; set; } = "5180490394:AAFXLACQWY4sXejU-7j4aRKBJzJkb7Mtv8Y";
        public static TelegramBotClient client;
        private static string Token { get; set; } = Settings.Token; //@KuzGTU_bot
        private static string connStr = Settings.connStr;

        [Obsolete]
        public static void Main()
        {
            client = new TelegramBotClient(Token);
            client.StartReceiving();
            client.OnMessage += BotOnMessageReceived;
            Console.ReadLine();
            client.StopReceiving();
        }

        [Obsolete]
        public static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;

            SqlConnection conn = new SqlConnection(connStr);
            conn.Open();

            List<ClassGroup_Lessons> groupLessons;
            List<ClassGroup_Id> groupId;
            List<ClassRoom_Id> roomId;
            List<ClassRoom_Lessons> roomLessons;
            List<ClassTeacher_Lessons> teacherLessons;
            List<ClassTeacher_Id> teacherId;

            var client_api = new RestClient("https://portal.kuzstu.ru/api/");

            var requestGroup = new RestRequest("group?groupname");
            var requestRoom = new RestRequest("classrooms?classroomname");
            var requestTeacher = new RestRequest("teachers?teacher");

            var responseGroup = client_api.Execute(requestGroup);
            var responseRoom = client_api.Execute(requestRoom);
            var responseTeacher = client_api.Execute(requestTeacher);

            SqlCommand cmd_str_chat_id = new SqlCommand("SELECT [ChatID] FROM [Steps] " +
                "WHERE [ChatID]='" + message.Chat.Id + "'", conn);
            string str_chat_id = (string)cmd_str_chat_id.ExecuteScalar();

            int GroupNotFound = 88;
            int GroupLessonsFound = 88;
            int RoomNotFound = 88;
            int RoomLessonsFound = 88;
            int TeachNotFound = 88;
            int TeachLessonsFound = 88;

            int LessErrorType = 0;
            int RoomErrorType = 0;
            int TeachErrorType = 0;
            //=0 - челу вывело расписание; 
            //=1 - группа найдена, но рассписания нет;
            //=2 - группа в принципе не найдена, и расписания не может быть.

            if (str_chat_id != Convert.ToString(message.Chat.Id))
            {
                SqlCommand insert_user = new SqlCommand("INSERT INTO [Steps] ([ChatID], [Step_Number]) Values ('" + message.Chat.Id + "', 0)", conn);
                insert_user.ExecuteNonQuery();
            }

            SqlCommand cmd_select_step = new SqlCommand("SELECT [Step_Number] FROM [Steps] WHERE [ChatID]='" + message.Chat.Id + "'", conn);
            int select_step = (int)cmd_select_step.ExecuteScalar();

            if (select_step == 0)
            {
                switch (message.Text)
                {
                    case "/start":
                        Console.WriteLine($"Пришло сообщение с текстом: {message.Text}");
                        await client.SendTextMessageAsync(message.Chat.Id, "Приветствую тебя, странник мира под названием «Интернет»! Чем могу быть полезен? (/help - помощь)");
                        SqlCommand update_step_start = new SqlCommand("UPDATE Steps SET Step_Number=0 WHERE [ChatID]='" + message.Chat.Id + "'", conn);
                        update_step_start.ExecuteNonQuery();
                        break;

                    case "/lessons":
                        Console.WriteLine($"Пришло сообщение с текстом: {message.Text}");
                        await client.SendTextMessageAsync(message.Chat.Id, "Введите группу и желаемую дату. Формат входной строки группа:день-месяц");
                        SqlCommand update_step_less = new SqlCommand("UPDATE [Steps] SET [Step_Number] = 1 WHERE [ChatID]='" + message.Chat.Id + "'", conn);
                        update_step_less.ExecuteNonQuery();
                        goto less;

                    case "/room":
                        Console.WriteLine($"Пришло сообщение с текстом: {message.Text}");
                        await client.SendTextMessageAsync(message.Chat.Id, "Введите номер аудитории и желаемую дату. Формат входной строки аудитория:дата");
                        SqlCommand update_step_room = new SqlCommand("UPDATE [Steps] SET [Step_Number] = 1 WHERE [ChatID]='" + message.Chat.Id + "'", conn);
                        update_step_room.ExecuteNonQuery();
                        goto rooom;

                    case "/teacher":
                        Console.WriteLine($"Пришло сообщение с текстом: {message.Text}");
                        await client.SendTextMessageAsync(message.Chat.Id, "Введите преподавателя и желаемую дату. Формат входной строки полное ФИО:дата");
                        SqlCommand update_step_teach = new SqlCommand("UPDATE [Steps] SET [Step_Number] = 1 WHERE [ChatID]='" + message.Chat.Id + "'", conn);
                        update_step_teach.ExecuteNonQuery();
                        goto teach;

                    case "/help":
                        Console.WriteLine($"Пришло сообщение с текстом: {message.Text}");
                        await client.SendTextMessageAsync(message.Chat.Id, "/start — начало общения с KuzBot\n" +
                        "/lessons — просмотр расписания на сегодняшний день, указав свою группу\n" +
                        "/room — просмотр преподавателей, находящихся в указанной аудитории\n" +
                        "/teacher — просмотр аудитории преподавателя по выбранной дате\n" +
                        "/stop — остановка бота\n" +
                        "/help — справка\n" +
                        "imroo1557@gmail.com - поддержка");
                        break;

                    case "/stop":
                        SqlCommand update_step_stop = new SqlCommand("UPDATE Steps SET Step_Number=0 WHERE [ChatID]='" + message.Chat.Id + "'", conn);
                        update_step_stop.ExecuteNonQuery();
                        break;

                    default:
                        await client.SendTextMessageAsync(message.Chat.Id, "Команда не распознана.\n" +
                        "Вы можете изменить запрос или воспользоваться командой помощи \n" +
                        "↓ ↓ ↓\n" +
                        "/help — справка\n");
                        SqlCommand update_step_0 = new SqlCommand("UPDATE Steps SET Step_Number=0 WHERE [ChatID]='" + message.Chat.Id + "'", conn);
                        update_step_0.ExecuteNonQuery();
                        break;
                }
            }

        less:
            {
                if (select_step == 1)
                {
                    if (message.Text != null && message.Text.Contains(':'))
                    {
                        string text = message.Text;
                        string[] words = text.Split(new char[] { ':' });
                        string your_group = words[0].Replace(" ", "");
                        string UDate = words[1].Replace(" ", "");

                        if (your_group.Contains('-'))
                        {
                            string your_groupID = "";
                            int GroupFound = 0;
                            int GroupLessonsFound2 = 0;
                            //string your_group = "";
                            if (responseGroup.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                //your_group = message.Text;
                                string rawResponseeGroup = responseGroup.Content;
                                groupId = JsonConvert.DeserializeObject<List<ClassGroup_Id>>(rawResponseeGroup);
                                GroupLessonsFound = groupId.Count;
                                foreach (var i in groupId)
                                {
                                    try
                                    {
                                        if (i.name == your_group ||
                                            i.name.ToLower() == your_group ||
                                            i.name.ToUpper() == your_group)
                                        {
                                            your_groupID = i.dept_id;
                                            GroupFound++;
                                        }
                                    }
                                    catch { };
                                    GroupLessonsFound--;
                                }
                                if (GroupLessonsFound == 0 && GroupFound == 0)
                                {
                                    LessErrorType = 2;
                                }
                            }

                            var LessRequest = new RestRequest("student_schedule?group_id=" + your_groupID);
                            var LessResponse = client_api.Execute(LessRequest);
                            if (LessResponse.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                string LessRawResponse = LessResponse.Content;
                                groupLessons = JsonConvert.DeserializeObject<List<ClassGroup_Lessons>>(LessRawResponse);
                                GroupNotFound = groupLessons.Count;

                                foreach (var i in groupLessons)
                                {
                                    try
                                    {
                                        var input_date = DateTime.Parse(UDate);
                                        var today = input_date.ToString("yyyy-MM-dd");
                                        //var today = DateTime.Today.ToString("yyyy-MM-dd");
                                        if (i.education_group_name.ToLower() == your_group.ToLower()
                                        && i.date_lesson == today)
                                        {
                                            GroupLessonsFound2++;
                                            string teacher_name = i.teacher_name;
                                            string subject = i.subject;
                                            string place = i.place;
                                            string type = i.type;
                                            string education_group_name = i.education_group_name;
                                            string lesson_number = i.lesson_number;
                                            await client.SendTextMessageAsync(message.Chat.Id, $"\n {today}\n" +
                                        $"{education_group_name}, {lesson_number} пара, {place}а\n" +
                                        $"{type} {subject}\n{teacher_name}");
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine("Авнимание! Ашипка: " + e.Message);
                                        return;
                                    };
                                    GroupNotFound--;
                                }
                                if (GroupNotFound == 0 && GroupLessonsFound2 == 0 && LessErrorType != 2)
                                {
                                    LessErrorType = 1;
                                }
                            }
                        }
                    }
                    if (GroupNotFound == 0)
                    {
                        SqlCommand update_step_l = new SqlCommand("UPDATE Steps SET Step_Number=0 WHERE [ChatID]='" + message.Chat.Id + "'", conn);
                        update_step_l.ExecuteNonQuery();

                        if (LessErrorType == 1)
                        {
                            await client.SendTextMessageAsync(message.Chat.Id, $"\n Группа найдена, но на запрашиваемый день расписание отсутствует");
                        }
                        if (LessErrorType == 2)
                        {
                            await client.SendTextMessageAsync(message.Chat.Id, $"\n Такой группы нет ( -_-)");
                        }
                    }
                }
            }

        rooom:
            {
                if (select_step == 1)
                {
                    if (message.Text != null && message.Text.Contains(':'))
                    {
                        string roomID = "";
                        string text = message.Text;
                        string[] words = text.Split(new char[] { ':' });
                        string your_room = words[0].Replace(" ", "");
                        string UDate = words[1].Replace(" ", "");
                        int RoomFound = 0;
                        int RoomLessonsFound2 = 0;

                        if (!your_room.Contains('-') && !your_room.Contains(' '))
                        {
                            if (responseRoom.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                //your_room = message.Text;
                                string rawResponseRoom = responseRoom.Content;
                                roomId = JsonConvert.DeserializeObject<List<ClassRoom_Id>>(rawResponseRoom);
                                RoomLessonsFound = roomId.Count;
                                foreach (var i in roomId)
                                {
                                    try
                                    {
                                        if (i.name == your_room ||
                                            i.name.ToLower() == your_room ||
                                            i.name.ToUpper() == your_room)
                                        {
                                            roomID = i.auditorium_id;
                                            RoomFound++;
                                        }
                                    }
                                    catch { };
                                    RoomLessonsFound--;
                                }
                                if (RoomLessonsFound == 0 && RoomFound == 0)
                                {
                                    RoomErrorType = 2;
                                }
                            }

                            var RoomRequest = new RestRequest("classroom_schedule?classroom=" + your_room);
                            var RoomResponse = client_api.Execute(RoomRequest);
                            if (RoomResponse.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                string RoomRawResponse = RoomResponse.Content;
                                roomLessons = JsonConvert.DeserializeObject<List<ClassRoom_Lessons>>(RoomRawResponse);
                                RoomNotFound = roomLessons.Count;
                                foreach (var i in roomLessons)
                                {
                                    try
                                    {
                                        var input_date = DateTime.Parse(UDate);
                                        var today = input_date.ToString("yyyy-MM-dd");
                                        if (i.date_lesson == today)
                                        {
                                            RoomLessonsFound2++;
                                            string teacher_name = i.teacher_name;
                                            string subject = i.subject;
                                            string place = i.place;
                                            string type = i.type;
                                            string education_group_name = i.education_group_name;
                                            string lesson_number = i.lesson_number;
                                            await client.SendTextMessageAsync(message.Chat.Id, $"\n {today}\n" +
                                        $"{education_group_name}, {lesson_number} пара, {place}а\n" +
                                        $"{type} {subject}\n{teacher_name}");
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine("Авнимание! Ашипка: " + e.Message);
                                        return;
                                    };
                                    RoomNotFound--;
                                }
                                if (RoomNotFound == 0 && RoomLessonsFound2 == 0 && RoomErrorType != 2)
                                {
                                    RoomErrorType = 1;
                                }
                            }
                        }
                    }
                    if (RoomNotFound == 0)
                    {
                        SqlCommand update_step_r = new SqlCommand("UPDATE Steps SET Step_Number=0 WHERE [ChatID]='" + message.Chat.Id + "'", conn);
                        update_step_r.ExecuteNonQuery();

                        if (RoomErrorType == 1)
                        {
                            await client.SendTextMessageAsync(message.Chat.Id, $"\n Аудитория найдена, но на запрашиваемый день расписание отсутствует");
                        }
                        if (RoomErrorType == 2)
                        {
                            await client.SendTextMessageAsync(message.Chat.Id, $"\n Такой аудитории нет ( -_-)");
                        }
                    }
                }
            }

        teach:
            {
                if (select_step == 1)
                {
                    if (message.Text != null && message.Text.Contains(':'))
                    {
                        string your_teacherID = "";
                        string text = message.Text;
                        string[] words = text.Split(new char[] { ':' });
                        string your_teacher = words[0];
                        string UDate = words[1].Replace(" ", "");
                        int TeachFound = 0;
                        int TeachLessonsFound2 = 0;
                        if (!your_teacher.Contains('-') && !your_teacher.All(char.IsDigit) && your_teacher.Contains(' '))
                        {
                            if (responseTeacher.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                //your_teacher = message.Text;
                                string rawResponseeTeacher = responseTeacher.Content;
                                teacherId = JsonConvert.DeserializeObject<List<ClassTeacher_Id>>(rawResponseeTeacher);
                                TeachLessonsFound = teacherId.Count;
                                foreach (var i in teacherId)
                                {
                                    try
                                    {
                                        if (i.name == your_teacher ||
                                            i.name.ToLower() == your_teacher ||
                                            i.name.ToUpper() == your_teacher)
                                        {
                                            your_teacherID = i.person_id;
                                            TeachFound++;
                                        }
                                    }
                                    catch { };
                                    TeachLessonsFound--;
                                }
                                if (TeachLessonsFound == 0 && TeachFound == 0)
                                {
                                    TeachErrorType = 2;
                                }
                            }

                            var TeachRequest = new RestRequest("teacher_schedule?teacher_id=" + your_teacherID);
                            var TeachResponse = client_api.Execute(TeachRequest);
                            if (TeachResponse.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                string TeachRawResponse = TeachResponse.Content;
                                teacherLessons = JsonConvert.DeserializeObject<List<ClassTeacher_Lessons>>(TeachRawResponse);
                                TeachNotFound = teacherLessons.Count;
                                foreach (var i in teacherLessons)
                                {
                                    try
                                    {
                                        var input_date = DateTime.Parse(UDate);
                                        var today = input_date.ToString("yyyy-MM-dd");
                                        if (i.date_lesson == today)
                                        {
                                            TeachLessonsFound2++;
                                            string subject = i.subject;
                                            string place = i.place;
                                            string type = i.type;
                                            string education_group_name = i.education_group_name;
                                            string lesson_number = i.lesson_number;
                                            await client.SendTextMessageAsync(message.Chat.Id, $"\n{your_teacher}\n {today}\n" +
                                        $"{education_group_name}, {lesson_number} пара, {place}а\n" +
                                        $"{type} {subject}\n");
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine("Авнимание! Ашипка: " + e.Message);
                                        return;
                                    };
                                    TeachNotFound--;
                                }
                                if (TeachNotFound == 0 && TeachLessonsFound2 == 0 && TeachErrorType != 2)
                                {
                                    TeachErrorType = 1;
                                }
                            }
                        }
                    }
                    if (TeachNotFound == 0)
                    {
                        SqlCommand update_step_t = new SqlCommand("UPDATE Steps SET Step_Number=0 WHERE [ChatID]='" + message.Chat.Id + "'", conn);
                        update_step_t.ExecuteNonQuery();

                        if (TeachErrorType == 1)
                        {
                            await client.SendTextMessageAsync(message.Chat.Id, $"\n Преподаватель найден, но на запрашиваемый день расписание отсутствует");
                        }
                        if (TeachErrorType == 2)
                        {
                            await client.SendTextMessageAsync(message.Chat.Id, $"\n Такого преподавателя нет ( -_-)");
                        }
                    }
                }
            }
            conn.Close();
        }
    }

    public class ClassGroup_Lessons
    {
        public string id { get; set; }
        public string education_group_name { get; set; }
        public string education_group_id { get; set; }
        public string day_number { get; set; }
        public string lesson_number { get; set; }
        public string place { get; set; }
        public string subgroup { get; set; }
        public string teacher_id { get; set; }
        public string teacher_name { get; set; }
        public string subject { get; set; }
        public string type { get; set; }
        public string date_lesson { get; set; }
    }

    public class ClassGroup_Id
    {
        public string dept_id { get; set; }
        public string name { get; set; }
    }

    public class ClassRoom_Id
    {
        public string auditorium_id { get; set; }
        public string name { get; set; }
    }

    public class ClassRoom_Lessons
    {
        public string id { get; set; }
        public string education_group_name { get; set; }
        public string education_group_id { get; set; }
        public string day_number { get; set; }
        public string lesson_number { get; set; }
        public string place { get; set; }
        public string subgroup { get; set; }
        public string teacher_id { get; set; }
        public string teacher_name { get; set; }
        public string subject { get; set; }
        public string type { get; set; }
        public string date_lesson { get; set; }
    }

    public class ClassTeacher_Lessons
    {
        public string id { get; set; }
        public string education_group_name { get; set; }
        public string education_group_id { get; set; }
        public string day_number { get; set; }
        public string lesson_number { get; set; }
        public string place { get; set; }
        public string subgroup { get; set; }
        public string subject { get; set; }
        public string type { get; set; }
        public string date_lesson { get; set; }
    }

    public class ClassTeacher_Id
    {
        public string person_id { get; set; }
        public string name { get; set; }
    }
}