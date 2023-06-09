﻿using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using EJ.AttendanceManagement;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace EJ.MainMenu
{
    public partial class AttendanceTeacherPage : Page
    {
        public ObservableCollection<Students> Students { get; set; }
        public AttendanceTeacherPage()
        {
            InitializeComponent();
            ComboSubject.ItemsSource = BDEntities.GetContext().Subjects.ToList();
            ComboGroup.ItemsSource = BDEntities.GetContext().Groups.ToList();
            SetYearComboBox();
            LoadStudents();
            TeacherPassManagementVisibility();
        }
        private void LoadStudents()
        {
            using (var db = new BDEntities())
            {
                var students = db.Students.Include("Users").ToList();
                Students = new ObservableCollection<Students>(students);
            }
            DataContext = this;
        }
        private void TeacherPassManagementVisibility()
        {
            int currentUser = (int)Application.Current.Properties["UserId"];

            int currentTeacherId = GetTeacherIdByUserId(currentUser);
            if (currentTeacherId != 0 && ComboSubject.SelectedItem is Subjects selectedSubject)
            {
                int selectedSubjectId = selectedSubject.SubjectId;
                bool isAssignedSubject = IsUserAssignedToSubject(currentTeacherId, selectedSubjectId);

                if (isAssignedSubject)
                {
                    PassManagement.Visibility = Visibility.Visible;
                }
                else
                {
                    PassManagement.Visibility = Visibility.Collapsed;
                }
            }
            int GetTeacherIdByUserId(int userId)
            {
                using (var context = new BDEntities())
                {
                    var teacher = context.Teachers.FirstOrDefault(t => t.UserId == userId);
                    if (teacher != null)
                    {
                        return teacher.TeacherId;
                    }
                    return 0;
                }
            }

            bool IsUserAssignedToSubject(int teacherId, int subjectId)
            {
                using (var context = new BDEntities())
                {
                    return context.TeachersBySubjects.Any(a => a.TeacherId == teacherId && a.SubjectId == subjectId);
                }
            }
        }
        private void SetYearComboBox()
        {
            int currentYear = DateTime.Now.Year;
            for (int year = 2019; year <= currentYear; year++)
            {
                ComboYear.Items.Add(year);
            }
            ComboYear.SelectedItem = currentYear;
        }

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            int currentMonthIndex = DateTime.Now.Month - 1;
            ComboBox ComboMonth = sender as ComboBox;
            ComboMonth.SelectedIndex = currentMonthIndex;
        }

        private void PassManagement_Click(object sender, RoutedEventArgs e)
        {
            if (ComboGroup.SelectedItem != null && ComboSubject.SelectedItem != null)
            {
                var window = new PassManagement(((Groups)ComboGroup.SelectedItem).GroupName, ((Subjects)ComboSubject.SelectedItem).SubjectName);
                window.ShowDialog();
                LoadGrid();
            }
            else
                MessageBox.Show("Выберите группу и предмет!");
        }

        private void LoadGrid()
        {
            if (!(ComboGroup.SelectedItem is Groups selectedGroup))
                return;

            if (!(ComboYear.SelectedItem is int selectedYear))
                return;

            if (ComboMonth.SelectedIndex < 0)
                return;

            int selectedMonth = ComboMonth.SelectedIndex + 1;

            DateTime startDate = new DateTime(selectedYear, selectedMonth, 1);
            DateTime endDate = startDate.AddMonths(1).AddDays(-1);

            int subjectId = (ComboSubject.SelectedItem as Subjects)?.SubjectId ?? 0;

            using (var db = new BDEntities())
            {
                var query = from s in db.Students
                            join g in db.Groups on s.GroupId equals g.GroupId
                            join u in db.Users on s.UserId equals u.UserId
                            join a in db.Attendance.Where(a => a.Date >= startDate && a.Date <= endDate && a.SubjectId == subjectId)
                                on s.StudentId equals a.StudentId into aGroup
                            from a in aGroup.DefaultIfEmpty()
                            join sub in db.Subjects on a.SubjectId equals sub.SubjectId into subGroup
                            from sub in subGroup.DefaultIfEmpty()
                            let passType = a != null && a.PassType
                            where g.GroupName == selectedGroup.GroupName
                            orderby s.StudentId
                            select new { u.UserName, s.StudentId, Date = (a != null ? a.Date : default(DateTime?)), HasAbsence = (a != null), SubjectName = (sub != null ? sub.SubjectName : ""), PassType = passType };

                var employeeAttendanceList = new List<EmployeeAttendance>();
                var employeeAttendance = new EmployeeAttendance();
                var lastEmpID = -1;

                foreach (var row in query.ToList())
                {
                    var empID = row.StudentId;
                    var day = row.Date?.Day ?? 0;

                    if (empID != lastEmpID)
                    {
                        if (lastEmpID != -1)
                        {
                            employeeAttendanceList.Add(employeeAttendance);
                        }
                        employeeAttendance = new EmployeeAttendance { Name = row.UserName };
                        lastEmpID = empID;
                    }

                    var passType = row.HasAbsence ? (row.PassType ? "УП" : "HП") : "";
                    var dayIndex = day - 1;
                    if (dayIndex >= 0 && dayIndex < employeeAttendance.Days.Count)
                    {
                        employeeAttendance.Days[dayIndex] = passType;
                    }
                }
                employeeAttendanceList.Add(employeeAttendance);
                myDataGrid.ItemsSource = employeeAttendanceList;
            }
        }
        private void CreateTable()
        {
            myDataGrid.Columns.Clear();
            myDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "ФИО",
                Binding = new Binding("Name"),
                IsReadOnly = true
            });
            int selectedMonthIndex = ComboMonth.SelectedIndex;

            if (selectedMonthIndex >= 0 && ComboYear.SelectedItem != null)
            {
                int selectedMonth = selectedMonthIndex + 1;
                int selectedYear = (int)ComboYear.SelectedItem;

                int daysInMonth = DateTime.DaysInMonth(selectedYear, selectedMonth);

                for (int i = 1; i <= daysInMonth; i++)
                {
                    var column = new DataGridTextColumn
                    {
                        Header = i.ToString(),
                        IsReadOnly = true
                    };

                    var binding = new Binding($"Days[{i - 1}]");
                    column.Binding = binding;

                    myDataGrid.Columns.Add(column);
                }
            }

            myDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Отсутствие\nпо неуважительной\nпричине",
                Binding = new Binding("DisrespectfulReason"),
                IsReadOnly = true
            });

            myDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Отсутствие\nпо уважительной\nпричине",
                Binding = new Binding("ValidReason"),
                IsReadOnly = true
            });
        }
        private void Combo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadGrid();
            CreateTable();
            TeacherPassManagementVisibility();
        }

        private void Graphs_Click(object sender, RoutedEventArgs e)
        {
            if (ComboGroup.SelectedItem != null && ComboSubject.SelectedItem != null)
            {
                var report = new Report(((Groups)ComboGroup.SelectedItem).GroupName, ((Subjects)ComboSubject.SelectedItem).SubjectName, (int)ComboYear.SelectedItem, (int)ComboMonth.SelectedIndex);
                report.Show();
                LoadGrid();
            }
            else
                MessageBox.Show("Выберите группу и предмет!");
        }

        private void ExportToWord_Click(object sender, RoutedEventArgs e)
        {
            string selectedMonthText = ((ComboBoxItem)ComboMonth.SelectedItem).Content.ToString();

            int selectedMonth = ComboMonth.SelectedIndex + 1;
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string folderName = "Посещаемость-Отчет";
            string folderPath = Path.Combine(desktopPath, folderName);
            Directory.CreateDirectory(folderPath);

            if (ComboGroup.SelectedItem != null && ComboSubject.SelectedItem != null)
            {
                try
                {
                    using (var db = new BDEntities())
                    {
                        string groupName = ((Groups)ComboGroup.SelectedItem).GroupName;
                        var queryInStudents = from s in db.Students
                                              join g in db.Groups on s.GroupId equals g.GroupId
                                              join u in db.Users on s.UserId equals u.UserId
                                              select new { u.UserName, g.GroupName, s.StudentId };
                        var students = queryInStudents.Where(s => s.GroupName == groupName).ToList();
                        var subject = ComboSubject.SelectedItem as Subjects;
                        var queryInAttendence = from a in db.Attendance
                                                join s in db.Students on a.StudentId equals s.StudentId
                                                join g in db.Groups on s.GroupId equals g.GroupId
                                                select new { a.AttendanceId, g.GroupName, s.StudentId, a.SubjectId, a.Date, a.PassType };
                        var attendance = queryInAttendence.Where(s => s.GroupName == groupName && s.SubjectId == subject.SubjectId && s.Date.Month == selectedMonth).ToList();

                        string fileName = $"{subject.SubjectName} - {groupName} - {selectedMonthText} {ComboYear.SelectedItem}.docx".Replace('/', '-');
                        string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
                        string cleanedFileName = new string(fileName.Where(x => !invalidChars.Contains(x)).ToArray());
                        string path = Path.Combine(folderPath, cleanedFileName);
                        using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document))
                        {
                            MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();

                            StyleDefinitionsPart styleDefinitionsPart = mainPart.AddNewPart<StyleDefinitionsPart>();
                            styleDefinitionsPart.Styles = new Styles();
                            styleDefinitionsPart.Styles.Save();

                            Document doc = new Document();
                            Body body = new Body();
                            Paragraph orientation = new Paragraph(new ParagraphProperties(new SectionProperties(new PageSize()
                            {
                                Width = (UInt32Value)15840U,
                                Height = (UInt32Value)12240U,
                                Orient = PageOrientationValues.Landscape
                            },
                            new PageMargin())));
                            Paragraph paraTitle = new Paragraph(new Run(new Text(cleanedFileName.Replace(".docx", ""))))
                            {
                                ParagraphProperties = new ParagraphProperties(
                                new Justification() { Val = JustificationValues.Center })
                            };
                            body.Append(paraTitle);

                            //Добавляем таблицу
                            Table table = new Table();
                            TableProperties tblProp = new TableProperties(new TableWidth() { Width = "100%", Type = TableWidthUnitValues.Pct },
                                new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Center });
                            tblProp.Append();

                            TableBorders borders = new TableBorders
                            {
                                TopBorder = new TopBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                                BottomBorder = new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                                LeftBorder = new LeftBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                                RightBorder = new RightBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                                InsideHorizontalBorder = new InsideHorizontalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                                InsideVerticalBorder = new InsideVerticalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 }
                            };
                            tblProp.Append(borders);

                            table.AppendChild(tblProp);

                            TableRow tr = new TableRow();
                            TableCell th = new TableCell();
                            Paragraph p = new Paragraph(new Run(new Text("ФИО студента")));

                            TableCellProperties thProps = new TableCellProperties(
                                new TableCellWidth() { Width = "10%" },
                                new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Center });
                            th.Append(thProps);

                            p.ParagraphProperties = new ParagraphProperties(new Justification() { Val = JustificationValues.Center });
                            th.Append(p);
                            tr.Append(th);

                            int numDaysInMonth = DateTime.DaysInMonth(DateTime.Now.Year, selectedMonth);

                            for (int day = 1; day <= numDaysInMonth; day++)
                            {
                                TableCell cell = new TableCell(new Paragraph(new Run(new Text(day.ToString()))));
                                TableCellProperties cellProps = new TableCellProperties(
                                    new TableCellWidth() { Width = "2%" },
                                    new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Center });
                                cell.Append(cellProps);
                                tr.Append(cell);
                            }
                            TableCell thValid = new TableCell(new Paragraph(new Run(new Text("По уважительной причиной"))));
                            TableCellProperties thValidProps = new TableCellProperties(new TableCellWidth() { Width = "3%" });
                            thValid.Append(thValidProps);
                            TableCell thInvalid = new TableCell(new Paragraph(new Run(new Text("Без уважительной причины"))));
                            TableCellProperties thInvalidProps = new TableCellProperties(new TableCellWidth() { Width = "3%" });
                            thInvalid.Append(thInvalidProps);
                            tr.Append(thValid);
                            tr.Append(thInvalid);
                            table.Append(tr);

                            foreach (var student in students)
                            {
                                TableRow trStudent = new TableRow();
                                string fullName = student.UserName;
                                string[] nameParts = fullName.Split(' ');
                                string fio;
                                if (nameParts.Length == 3)
                                {
                                    fio = $"{nameParts[0]} {nameParts[1][0]}.{nameParts[2][0]}.";
                                }
                                else
                                {
                                    fio = $"{nameParts[0]} {nameParts[1][0]}.";
                                }
                                TableCell tdName = new TableCell(new Paragraph(new Run(new Text(fio))));
                                trStudent.Append(tdName);

                                int validAbsences = attendance.Where(a => a.StudentId == student.StudentId && a.PassType == true).Count();
                                int invalidAbsences = attendance.Where(a => a.StudentId == student.StudentId && a.PassType == false).Count();

                                for (int day = 1; day <= DateTime.DaysInMonth(DateTime.Now.Year, selectedMonth); day++)
                                {

                                    var att = attendance.FirstOrDefault(x => x.Date.Day == day && x.StudentId == student.StudentId);
                                    string attString = att != null ? (att.PassType ? "②" : "2") : "";
                                    TableCell tdAttendance = new TableCell(new Paragraph(new Run(new Text(attString))));
                                    trStudent.Append(tdAttendance);
                                }
                                table.Append(trStudent);
                                validAbsences *= 2;
                                invalidAbsences *= 2;

                                TableCell tdValidAbsences = new TableCell(new Paragraph(new Run(new Text(validAbsences.ToString()))));
                                trStudent.Append(tdValidAbsences);

                                TableCell tdInvalidAbsences = new TableCell(new Paragraph(new Run(new Text(invalidAbsences.ToString()))));
                                trStudent.Append(tdInvalidAbsences);
                            }
                            foreach (TableRow row in table.Elements<TableRow>())
                            {
                                foreach (TableCell cell in row.Elements<TableCell>())
                                {
                                    ParagraphProperties props = new ParagraphProperties();
                                    Justification justification = new Justification() { Val = JustificationValues.Center };
                                    props.Append(justification);
                                    cell.Append(props);
                                }
                            }
                            body.Append(table);
                            body.Append(orientation);

                            doc.Append(body);
                            mainPart.Document = doc;

                            mainPart.Document.Save();
                            wordDoc.Dispose();

                            MessageBox.Show("Отчет успешно сохранен на рабочем столе", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);

                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при экспорте в Word: {ex.Message}");
                }
            }
            else
                MessageBox.Show("Выберите группу и предмет!");

        }
    }
}