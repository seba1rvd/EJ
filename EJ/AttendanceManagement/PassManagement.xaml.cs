﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EJ.AttendanceManagement
{
    /// <summary>
    /// Логика взаимодействия для PassManagement.xaml
    /// </summary>
    public partial class PassManagement : Window
    {
        public PassManagement()
        {
            InitializeComponent();
            ComboGroup.ItemsSource = BDEntities.GetContext().Groups.Local;
            ComboSubject.ItemsSource = BDEntities.GetContext().Subjects.ToList();
            ComboStudent.ItemsSource = Students;
        }
        private List<Students> Students
        {
            get
            {
                if (ComboGroup.SelectedItem != null)
                {
                    var selectedGroup = ComboGroup.SelectedItem as Groups;
                    return BDEntities.GetContext().Students.Where(s => s.GroupId == selectedGroup.GroupId).ToList();
                }
                return null;
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }

        private void ComboGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboStudent.ItemsSource = Students;
        }

        private void Add_attendance_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new BDEntities())
                {
                    var entity = ComboStudent.SelectedItem as Students;
                    var entity2 = ComboSubject.SelectedItem as Subjects;
                    context.Entry(entity).State = EntityState.Detached;

                    using (SqlConnection connection = new SqlConnection(@"Data Source=localhost\SQLEXPRESS;Initial Catalog=BD;Integrated Security=True"))
                    {
                        connection.Open();

                        using (SqlCommand command = new SqlCommand("SELECT * FROM Attendance WHERE StudentId = @Value AND Date = @Value1 AND SubjectId = @Value2", connection))
                        {
                            command.Parameters.AddWithValue("@Value", entity.StudentId);
                            command.Parameters.AddWithValue("@Value1", datePicker.SelectedDate.Value.ToString("yyyy-MM-dd"));
                            command.Parameters.AddWithValue("@Value2", entity2.SubjectId);

                            SqlDataReader reader = command.ExecuteReader();

                            if (reader.Read())
                            {
                                bool previousPassType = reader.GetBoolean(reader.GetOrdinal("PassType"));
                                bool newPassType = false;

                                if (ComboPassType.SelectedIndex == 0) // Отсутствие по неуважительной причине
                                {
                                    newPassType = false;
                                }
                                else if (ComboPassType.SelectedIndex == 1) // Отсутствие по уважительной причине
                                {
                                    newPassType = true;
                                }
                                else if (ComboPassType.SelectedIndex == 2) // Удаление пропуска
                                {
                                    reader.Close();

                                    using (SqlCommand deleteCommand = new SqlCommand("DELETE FROM Attendance WHERE StudentId = @Value AND Date = @Value1 AND SubjectId = @Value2", connection))
                                    {
                                        deleteCommand.Parameters.AddWithValue("@Value", entity.StudentId);
                                        deleteCommand.Parameters.AddWithValue("@Value1", datePicker.SelectedDate.Value.ToString("yyyy-MM-dd"));
                                        deleteCommand.Parameters.AddWithValue("@Value2", entity2.SubjectId);
                                        deleteCommand.ExecuteNonQuery();
                                        MessageBox.Show("Пропуск удален.");
                                    }
                                    return;
                                }

                                if (previousPassType != newPassType) // Если тип пропуска изменился
                                {
                                    reader.Close();

                                    using (SqlCommand updateCommand = new SqlCommand("UPDATE Attendance SET PassType = @Value3 WHERE StudentId = @Value AND Date = @Value1 AND SubjectId = @Value2", connection))
                                    {
                                        updateCommand.Parameters.AddWithValue("@Value", entity.StudentId);
                                        updateCommand.Parameters.AddWithValue("@Value1", datePicker.SelectedDate.Value.ToString("yyyy-MM-dd"));
                                        updateCommand.Parameters.AddWithValue("@Value2", entity2.SubjectId);
                                        updateCommand.Parameters.AddWithValue("@Value3", newPassType);
                                        updateCommand.ExecuteNonQuery();
                                        MessageBox.Show("Пропуск обновлен.");
                                    }
                                }
                                else
                                {
                                    reader.Close();
                                    MessageBox.Show("Пропуск уже существует.");
                                }
                            }
                            else
                            {
                                reader.Close();


                                if (ComboPassType.SelectedIndex == 2) // Удаление пропуска, но его нет
                                {
                                    MessageBox.Show("Пропуска нет, чтобы его удалить.");
                                    return;
                                }

                                using (SqlCommand insertCommand = new SqlCommand("INSERT INTO Attendance (StudentId, Date, SubjectId, PassType) VALUES (@Value,  @Value1, @Value2, @Value3)", connection))
                                {
                                    if (datePicker.SelectedDate != null && entity != null)
                                    {
                                        bool passType = false;

                                        if (ComboPassType.SelectedIndex == 0) // Отсутствие по неуважительной причине
                                        {
                                            passType = false;
                                        }
                                        else if (ComboPassType.SelectedIndex == 1) // Отсутствие по уважительной причине
                                        {
                                            passType = true;
                                        }

                                        insertCommand.Parameters.AddWithValue("@Value", entity.StudentId);
                                        insertCommand.Parameters.AddWithValue("@Value1", datePicker.SelectedDate.Value.ToString("yyyy-MM-dd"));
                                        insertCommand.Parameters.AddWithValue("@Value2", entity2.SubjectId);
                                        insertCommand.Parameters.AddWithValue("@Value3", passType);
                                        insertCommand.ExecuteNonQuery();
                                        MessageBox.Show("Пропуск добавлен.");
                                    }
                                    else
                                    {
                                        MessageBox.Show("Пожалуйста, выберите действительного студента и дату из комбо-боксов.");
                                    }
                                }
                            }

                            connection.Close();
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения данных: " + ex.Message);
            }
        }


        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
