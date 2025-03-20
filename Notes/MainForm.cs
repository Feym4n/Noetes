using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Drawing.Imaging;

namespace Notes
{
    /// <summary>
    /// Главная форма приложения Notes.
    /// Предоставляет функциональность для создания, редактирования и управления заметками.
    /// </summary>
    public partial class MainForm : Form
    {
        #region Private Fields
        private bool fileAlreadySaved;      // Флаг, указывающий, был ли файл уже сохранен
        private bool fileUpdated;           // Флаг, указывающий, были ли внесены изменения в текущий файл
        private string currentfilename;     // Путь к текущему открытому файлу
        private FontDialog fontDialog;      // Диалог выбора шрифта
        private List<NoteInfo> notes;       // Список всех заметок
        private string notesDirectory;      // Путь к директории с заметками
        #endregion

        #region Constructor
        /// <summary>
        /// Инициализирует новый экземпляр формы MainForm.
        /// Создает директорию для заметок и загружает существующие заметки.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            
            // Инициализируем список заметок
            notes = new List<NoteInfo>();
            
            // Устанавливаем путь к директории с заметками
            string appPath = Application.StartupPath;
            if (string.IsNullOrEmpty(appPath))
            {
                appPath = AppDomain.CurrentDomain.BaseDirectory;
            }
            notesDirectory = Path.Combine(appPath, "Notes");
            
            InitializeDataGridView();
            EnsureNotesDirectory();
            LoadNotesList();
            
            // Создаем первую заметку при первом запуске приложения
            if (notes.Count == 0)
            {
                CreateFirstNote();
            }
        }
        #endregion

        #region DataGridView Methods
        /// <summary>
        /// Инициализирует DataGridView для отображения списка заметок.
        /// Настраивает внешний вид и поведение таблицы.
        /// </summary>
        private void InitializeDataGridView()
        {
            notesDataGridView.Dock = DockStyle.Left;
            notesDataGridView.Width = 300;
            notesDataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            notesDataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            notesDataGridView.MultiSelect = false;
            notesDataGridView.ReadOnly = true;
            notesDataGridView.AllowUserToAddRows = false;
            notesDataGridView.AllowUserToDeleteRows = false;

            // Добавляем колонки для отображения информации о заметках
            notesDataGridView.Columns.Add("FileName", "Название заметки");
            notesDataGridView.Columns.Add("LastModified", "Время сохранения");

            notesDataGridView.DoubleClick += NotesDataGridView_DoubleClick;

            // Создаем контекстное меню для удаления заметок
            CreateContextMenu();
        }

        /// <summary>
        /// Создает контекстное меню для DataGridView.
        /// </summary>
        private void CreateContextMenu()
        {
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            ToolStripMenuItem deleteMenuItem = new ToolStripMenuItem("Удалить");
            deleteMenuItem.Click += DeleteNote_Click;
            contextMenu.Items.Add(deleteMenuItem);
            notesDataGridView.ContextMenuStrip = contextMenu;
        }
        #endregion

        #region File Operations
        /// <summary>
        /// Создает первую заметку при первом запуске приложения.
        /// </summary>
        private void CreateFirstNote()
        {
            string firstNotePath = Path.Combine(notesDirectory, "Первая заметка.rtf");
            MainRichTextBox.Text = "Добро пожаловать в приложение Notes!\n\n" +
                                 "Это ваша первая заметка. Вы можете отредактировать этот текст " +
                                 "или создать новую заметку.";
            MainRichTextBox.SaveFile(firstNotePath, RichTextBoxStreamType.RichText);
            LoadNotesList();
        }

        /// <summary>
        /// Проверяет существование директории для заметок и создает её при необходимости.
        /// </summary>
        private void EnsureNotesDirectory()
        {
            if (!Directory.Exists(notesDirectory))
            {
                Directory.CreateDirectory(notesDirectory);
            }
        }

        /// <summary>
        /// Загружает список всех заметок из директории в DataGridView.
        /// Сортирует заметки по времени последнего изменения.
        /// </summary>
        private void LoadNotesList()
        {
            notes.Clear();
            notesDataGridView.Rows.Clear();

            // Получаем все файлы заметок из директории
            var files = Directory.GetFiles(notesDirectory, "*.*")
                               .Where(f => f.EndsWith(".txt") || f.EndsWith(".rtf"));

            // Загружаем информацию о каждой заметке
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                notes.Add(new NoteInfo
                {
                    FilePath = file,
                    FileName = Path.GetFileName(file),
                    LastModified = fileInfo.LastWriteTime
                });
            }

            // Сортируем заметки по времени последнего изменения (новые сверху)
            notes = notes.OrderByDescending(n => n.LastModified).ToList();

            // Отображаем заметки в DataGridView
            foreach (var note in notes)
            {
                notesDataGridView.Rows.Add(note.FileName, note.LastModified);
            }
        }

        /// <summary>
        /// Загружает содержимое заметки в RichTextBox.
        /// </summary>
        /// <param name="filePath">Путь к файлу заметки</param>
        private void LoadNote(string filePath)
        {
            if (File.Exists(filePath))
            {
                // Определяем формат файла и загружаем его соответствующим способом
                if (Path.GetExtension(filePath) == ".txt")
                {
                    MainRichTextBox.LoadFile(filePath, RichTextBoxStreamType.PlainText);
                }
                else if (Path.GetExtension(filePath) == ".rtf")
                {
                    MainRichTextBox.LoadFile(filePath, RichTextBoxStreamType.RichText);
                }

                UpdateFormTitle(Path.GetFileName(filePath));
                fileAlreadySaved = true;
                fileUpdated = false;
                currentfilename = filePath;
            }
        }

        /// <summary>
        /// Обновляет заголовок формы.
        /// </summary>
        /// <param name="fileName">Имя файла для отображения в заголовке</param>
        private void UpdateFormTitle(string fileName)
        {
            this.Text = $"{fileName} — Notes";
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Обработчик двойного клика по строке в DataGridView.
        /// Открывает выбранную заметку в RichTextBox.
        /// </summary>
        private void NotesDataGridView_DoubleClick(object sender, EventArgs e)
        {
            if (notesDataGridView.CurrentRow != null)
            {
                var selectedNote = notes[notesDataGridView.CurrentRow.Index];
                LoadNote(selectedNote.FilePath);
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Выход".
        /// Закрывает приложение.
        /// </summary>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Новый".
        /// Создает новую заметку с запросом сохранения текущей, если есть изменения.
        /// </summary>
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (fileUpdated)
            {
                DialogResult result = MessageBox.Show(
                    "Вы хотите сохранить изменения?",
                    "Сохранение файла",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Information);

                switch (result)
                {
                    case DialogResult.Yes:
                        SaveFileUpdated();
                        ClearScreen();
                        break;
                    case DialogResult.No:
                        ClearScreen();
                        break;
                }
            }
            else
            {
                ClearScreen();
            }

            // Отключаем кнопки отмены/возврата для новой заметки
            undoToolStripMenuItem.Enabled = false;
            redoToolStripMenuItem.Enabled = false;
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Открыть".
        /// Позволяет открыть существующую заметку из файла.
        /// </summary>
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text Files (*.txt)|*.txt|Rich Text Files (*.rtf)|*.rtf";

            DialogResult result = openFileDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                // Загружаем файл в зависимости от его расширения
                if (Path.GetExtension(openFileDialog.FileName) == ".txt")
                {
                    MainRichTextBox.LoadFile(openFileDialog.FileName, RichTextBoxStreamType.PlainText);
                }
                if (Path.GetExtension(openFileDialog.FileName) == ".rtf")
                {
                    MainRichTextBox.LoadFile(openFileDialog.FileName, RichTextBoxStreamType.RichText);
                }
                UpdateFormTitle(Path.GetFileName(openFileDialog.FileName));

                fileAlreadySaved = true;
                fileUpdated = false;
                currentfilename = openFileDialog.FileName;
                LoadNotesList();
            }

            // Отключаем кнопки отмены/возврата после открытия файла
            undoToolStripMenuItem.Enabled = false;
            redoToolStripMenuItem.Enabled = false;
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Сохранить как".
        /// Позволяет сохранить текущую заметку под новым именем.
        /// </summary>
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            savefile();
        }

        /// <summary>
        /// Сохраняет текущую заметку в файл.
        /// Позволяет выбрать имя и формат файла.
        /// </summary>
        private void savefile()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = notesDirectory;
            saveFileDialog.Filter = "Text Files (*.txt)|*.txt|Rich Text Files (*.rtf)|*.rtf";
            DialogResult savefileresult = saveFileDialog.ShowDialog();

            if (savefileresult == DialogResult.OK)
            {
                // Сохраняем файл в зависимости от выбранного формата
                if (Path.GetExtension(saveFileDialog.FileName) == ".txt")
                {
                    MainRichTextBox.SaveFile(saveFileDialog.FileName, RichTextBoxStreamType.PlainText);
                }
                if (Path.GetExtension(saveFileDialog.FileName) == ".rtf")
                {
                    MainRichTextBox.SaveFile(saveFileDialog.FileName, RichTextBoxStreamType.RichText);
                }
                UpdateFormTitle(Path.GetFileName(saveFileDialog.FileName));

                fileAlreadySaved = true;
                fileUpdated = false;
                currentfilename = saveFileDialog.FileName;
                LoadNotesList();
            }
        }

        /// <summary>
        /// Обработчик события загрузки формы.
        /// Инициализирует начальное состояние приложения.
        /// </summary>
        private void MainForm_Load(object sender, EventArgs e)
        {
            fileAlreadySaved = false;
            fileUpdated = false;
            currentfilename = "";
        }

        /// <summary>
        /// Обработчик события изменения текста в RichTextBox.
        /// Отслеживает изменения в текущей заметке.
        /// </summary>
        private void MainRichTextBox_TextChanged(object sender, EventArgs e)
        {
            fileUpdated = true;
            undoToolStripMenuItem.Enabled = true;
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Сохранить".
        /// Сохраняет текущую заметку.
        /// </summary>
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileUpdated();
        }

        /// <summary>
        /// Сохраняет текущую заметку, если она уже была сохранена ранее.
        /// В противном случае вызывает диалог "Сохранить как".
        /// </summary>
        private void SaveFileUpdated()
        {
            if (fileAlreadySaved)
            {
                // Сохраняем в текущий файл
                if (Path.GetExtension(currentfilename) == ".txt")
                {
                    MainRichTextBox.SaveFile(currentfilename, RichTextBoxStreamType.PlainText);
                }
                if (Path.GetExtension(currentfilename) == ".rtf")
                {
                    MainRichTextBox.SaveFile(currentfilename, RichTextBoxStreamType.RichText);
                }

                fileUpdated = false;
                LoadNotesList();
            }
            else
            {
                if (fileUpdated)
                {
                    savefile();
                }
                else
                {
                    ClearScreen();
                }
            }
        }

        /// <summary>
        /// Очищает текущую заметку и создает новую строку в списке заметок.
        /// </summary>
        private void ClearScreen()
        {
            MainRichTextBox.Clear();
            fileUpdated = false;
            this.Text = "Notes";
            
            // Добавляем новую строку в DataGridView
            notesDataGridView.Rows.Insert(0, "Новая заметка", DateTime.Now);
            notesDataGridView.CurrentCell = notesDataGridView.Rows[0].Cells[0];
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Отменить".
        /// Отменяет последнее действие в редакторе.
        /// </summary>
        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainRichTextBox.Undo();
            undoToolStripMenuItem.Enabled = false;
            redoToolStripMenuItem.Enabled = true;
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Вернуть".
        /// Возвращает отмененное действие в редакторе.
        /// </summary>
        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainRichTextBox.Redo();
            undoToolStripMenuItem.Enabled = true;
            redoToolStripMenuItem.Enabled = false;
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Выделить все".
        /// Выделяет весь текст в редакторе.
        /// </summary>
        private void sellectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainRichTextBox.SelectAll();
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Дата и время".
        /// Вставляет текущую дату и время в позицию курсора.
        /// </summary>
        private void dateTimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainRichTextBox.SelectedText = DateTime.Now.ToString();
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Жирный".
        /// Применяет жирный стиль к выделенному тексту.
        /// </summary>
        private void bToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FontTextStyle(FontStyle.Bold);
        }

        /// <summary>
        /// Применяет указанный стиль шрифта к выделенному тексту.
        /// </summary>
        /// <param name="fontStyle">Стиль шрифта для применения</param>
        private void FontTextStyle(FontStyle fontStyle)
        {
            MainRichTextBox.SelectionFont = new Font(MainRichTextBox.Font, fontStyle);
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Курсив".
        /// Применяет курсивный стиль к выделенному тексту.
        /// </summary>
        private void italicToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FontTextStyle(FontStyle.Italic);
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Подчеркнутый".
        /// Применяет подчеркнутый стиль к выделенному тексту.
        /// </summary>
        private void underlineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FontTextStyle(FontStyle.Underline);
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Зачеркнутый".
        /// Применяет зачеркнутый стиль к выделенному тексту.
        /// </summary>
        private void strikthroughToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FontTextStyle(FontStyle.Strikeout);
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Обычный".
        /// Убирает все стили с выделенного текста.
        /// </summary>
        private void noramToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FontTextStyle(FontStyle.Regular);
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Шрифт".
        /// Открывает диалог выбора шрифта и цвета.
        /// </summary>
        private void formmateFonteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fontDialog.ShowColor = true;
            fontDialog.ShowApply = true;

            fontDialog.Apply += new System.EventHandler(font_Apply_Dialog);

            DialogResult result = fontDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                if (MainRichTextBox.SelectionLength > 0)
                {
                    MainRichTextBox.SelectionFont = fontDialog.Font;
                    MainRichTextBox.SelectionColor = fontDialog.Color;
                }
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Применить" в диалоге выбора шрифта.
        /// Применяет выбранный шрифт и цвет к выделенному тексту.
        /// </summary>
        private void font_Apply_Dialog(object sender, EventArgs e)
        {
            if (MainRichTextBox.SelectionLength > 0)
            {
                MainRichTextBox.SelectionFont = fontDialog.Font;
                MainRichTextBox.SelectionColor = fontDialog.Color;
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Цвет текста".
        /// Открывает диалог выбора цвета для выделенного текста.
        /// </summary>
        private void changeTextColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            DialogResult result = colorDialog.ShowDialog();

            if (result == DialogResult.OK)
            { 
                if (MainRichTextBox.SelectionLength > 0)
                {
                    MainRichTextBox.SelectionColor = colorDialog.Color;
                }
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Удалить" в контекстном меню.
        /// Удаляет выбранную заметку после подтверждения.
        /// </summary>
        private void DeleteNote_Click(object sender, EventArgs e)
        {
            if (notesDataGridView.CurrentRow != null)
            {
                var selectedNote = notes[notesDataGridView.CurrentRow.Index];
                
                DialogResult result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить заметку '{selectedNote.FileName}'?",
                    "Подтверждение удаления",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        File.Delete(selectedNote.FilePath);
                        LoadNotesList();

                        // Если удаляемая заметка была открыта, очищаем RichTextBox
                        if (currentfilename == selectedNote.FilePath)
                        {
                            ClearScreen();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении файла: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Вставить изображение".
        /// Позволяет вставить изображение из файла в текущую позицию курсора.
        /// </summary>
        private void InsertImage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Изображения|*.jpg;*.jpeg;*.png;*.gif;*.bmp";
                openFileDialog.Title = "Выберите изображение";
                
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (Image img = Image.FromFile(openFileDialog.FileName))
                        {
                            // Копируем изображение в буфер обмена
                            using (MemoryStream ms = new MemoryStream())
                            {
                                img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                ms.Position = 0;
                                
                                // Создаем Bitmap из потока
                                using (Bitmap bmp = new Bitmap(ms))
                                {
                                    // Копируем изображение в буфер обмена
                                    Clipboard.SetImage(bmp);
                                    
                                    // Вставляем изображение в текущую позицию курсора
                                    MainRichTextBox.Paste();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при вставке изображения: {ex.Message}", "Ошибка", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// Класс для хранения информации о заметке.
    /// </summary>
    public class NoteInfo
    {
        /// <summary>
        /// Полный путь к файлу заметки.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Имя файла заметки.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Время последнего изменения заметки.
        /// </summary>
        public DateTime LastModified { get; set; }
    }
}
