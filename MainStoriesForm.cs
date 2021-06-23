using Stories.Model;
using Stories.Services;
using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Stories
{
    public partial class MainStoriesForm : Form
    {
        private readonly string PATH = $"{Environment.CurrentDirectory}\\StoryContent.bin";
        private StoryContent storyContent = new StoryContent();
        private FileIOService fileIOService;

        private RectangleRibbonSelector ribbonSelector;
        private SelectionHolder selHolder;

        public MainStoriesForm()
        {
            InitializeComponent();
            StoryLibrary.Init();

            // создаём объект рамки выбора фигур
            ribbonSelector = new RectangleRibbonSelector(panStory, new Pen(Color.Fuchsia) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot });
            ribbonSelector.OnSelected += RibbonSelector_OnSelected;

            selHolder = new SelectionHolder(panStory);
        }

        private void RibbonSelector_OnSelected(object sender, RibbonSelectedEventArgs e)
        {
            var rect = e.RectangleSelected;
            selHolder.Clear();
            foreach (var control in panStory.Controls.Cast<Control>())
            {
                if (Rectangle.Intersect(control.Bounds, rect).IsEmpty) continue;
                selHolder.Add(control);
            }
            pgStoryElement.SelectedObject = null;
            pgStoryElement.SelectedObjects = selHolder.GetSelected();
        }

        /// <summary>
        /// При первой загрузке заполняем дерево библиотеки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainStoriesForm_Load(object sender, EventArgs e)
        {
            fileIOService = new FileIOService(PATH);
            try
            {
                storyContent.Items = fileIOService.LoadData();
                panStory.Controls.Clear();
                tvStory.Nodes.Clear();
                foreach (var element in storyContent.Items)
                {
                    panStory.Controls.Add(element);
                    AddEventHandlers(element);
                    tvStory.Nodes.Add(new TreeNode(element.Text) { Tag = element });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Close();
            }

            tvLibrary.Nodes.Clear();
            foreach (var type in StoryLibrary.GetControlTypes())
            {
                // имя типа составное и содержит имена namespace
                var names = $"{type}".Split('.');
                // поэтому забираем только крайнее правое, имя типа
                tvLibrary.Nodes.Add(new TreeNode(names[names.Length - 1]) 
                { 
                    Tag = type 
                });
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        bool isHold;
        Point downPressed;

        private void tvLibrary_MouseDown(object sender, MouseEventArgs e)
        {
            downPressed = e.Location;
            tvLibrary.SelectedNode = tvLibrary.GetNodeAt(downPressed);
            isHold = e.Button == MouseButtons.Left && tvLibrary.SelectedNode != null;
        }

        /// <summary>
        /// При перемещении нажатой левой кнопке указателя более чем на 5 единиц запускаем процесс перетаскивания
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tvLibrary_MouseMove(object sender, MouseEventArgs e)
        {
            // также проверяем, что тип элемента выбран в дереве библиотеки и лекая кнопка была нажата
            if (tvLibrary.SelectedNode != null && isHold &&
                (Math.Abs(downPressed.X - e.Location.X) > 5 || Math.Abs(downPressed.Y - e.Location.Y) > 5))
            {
                // запускаем процесс перетаскивания
                tvLibrary.DoDragDrop(tvLibrary.SelectedNode, DragDropEffects.Move);
                // снимаем выбор типа с узла библиотеки
                tvLibrary.SelectedNode = null;
            }
        }

        private void panStory_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void tvLibrary_MouseUp(object sender, MouseEventArgs e)
        {
            isHold = false;
        }

        /// <summary>
        /// Сбрасывание элементов на поле
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panStory_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Effect == DragDropEffects.Move)
            {
                // получаем ссылку на узел дерева, который в свойстве Tag имеет тип контрола
                var typeNode = (TreeNode)e.Data.GetData(typeof(TreeNode));
                // создаем контрол из переданного типа
                var element = (Control)Activator.CreateInstance((Type)typeNode.Tag);

                // присваиваем видимое тесктовое значение по умолчанию
                element.Text = typeNode.Text;
                // располагаем верхний левый угол в точке сбрасывания
                element.Location = panStory.PointToClient(new Point(e.X, e.Y));
                // добавляем новый контрол в список контролов контейнера
                panStory.Controls.Add(element);
                AddEventHandlers(element);
                storyContent.Items.Add(element);
                ContentChanged = true;
                // добавляем новый элемент в дерево проекта
                var controlNode = new TreeNode(element.Text) { Tag = element };
                tvStory.Nodes.Add(controlNode);
                // делаем его текущим
                tvStory.SelectedNode = controlNode;
            }
        }

        private void AddEventHandlers(Control element)
        {
            // прицепляем клик на вновь поставленный элемент
            element.Click += Element_Click;
            if (element is CheckBox checkBox)
                checkBox.CheckedChanged += ValueChanged_Click;
            if (element is RadioButton radioButton)
                radioButton.CheckedChanged += ValueChanged_Click;
            if (element is TextBox textBox)
                textBox.TextChanged += ValueChanged_Click;
        }

        private void ValueChanged_Click(object sender, EventArgs e)
        {
            ContentChanged = true;
        }

        private bool contentChanged;

        public bool ContentChanged
        {
            get { return contentChanged; }
            set 
            {
                if (contentChanged == value) return;
                contentChanged = value;
                saveAsToolStripMenuItem.Enabled = contentChanged;
                saveToolStripButton.Enabled = contentChanged;
            }
        }

        private void SaveContent()
        {
            fileIOService.SaveData(storyContent.Items);
            ContentChanged = false;
        }

        /// <summary>
        /// При клике на выставленном элементе
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Element_Click(object sender, EventArgs e)
        {
            // переключаем вкладку на проект
            tcSelector.SelectedTab = tabPage1;
            // ищем и выделяем узел, привязанный к элементу
            tvStory.SelectedNode = tvStory.Nodes.Cast<TreeNode>().FirstOrDefault(item => item.Tag == sender);
            // передаем его сетке свойств
            pgStoryElement.SelectedObject = sender;

            selHolder.Clear();
            selHolder.Add((Control)sender);
        }

        /// <summary>
        /// При выборе узла проекта, связанного с элементом
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tvStory_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node != null)
                // если выбор не пуст, то передаем элемент сетке свойств
                pgStoryElement.SelectedObject = e.Node.Tag;
            else
                pgStoryElement.SelectedObject = null;
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveContent();
        }

        private void pgStoryElement_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            ContentChanged = true;
        }
    }
}
