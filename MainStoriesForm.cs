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
        private FileIOService fileIOService;

        public MainStoriesForm()
        {
            InitializeComponent();
            StoryLibrary.Init();
            storyPad.Size = new Size(5000, 5000);
            panelCentral.Controls.Add(storyPad);
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
                storyPad.LoadData(fileIOService.LoadData());
                tvStory.Nodes.Clear();
                foreach (var element in storyPad.Elements)
                {
                    tvStory.Nodes.Add(new TreeNode(element.Text) { Tag = element });
                }
                storyPad.Invalidate();
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

        private void storyPad_DragOver(object sender, DragEventArgs e)
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
        private void storyPad_DragDrop(object sender, DragEventArgs e)
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
                element.Location = storyPad.PointToClient(new Point(e.X, e.Y));
                storyPad.Add(element);

                ContentChanged = true;
                // добавляем новый элемент в дерево проекта
                var controlNode = new TreeNode(element.Text) { Tag = element };
                tvStory.Nodes.Add(controlNode);
                // делаем его текущим
                tvStory.SelectedNode = controlNode;
            }
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
            fileIOService.SaveData(storyPad.Elements);
            ContentChanged = false;
        }

        Point downPoint;

        /// <summary>
        /// При клике на выставленном элементе
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Element_MouseDownClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                downPoint = e.Location;
            // переключаем вкладку на проект
            tcSelector.SelectedTab = tabPage1;
            // ищем и выделяем узел, привязанный к элементу
            tvStory.SelectedNode = tvStory.Nodes.Cast<TreeNode>().FirstOrDefault(item => item.Tag == sender);
            // передаем его сетке свойств
            pgStoryElement.SelectedObject = sender;
        }

        private void Element_MouseMoveClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && sender is Control control)
            {
                var pt = control.Location;
                pt.Offset(e.Location.X - downPoint.X, e.Location.Y - downPoint.Y);
                control.Location = pt;
                downPoint = e.Location;
            }
        }

        /// <summary>
        /// При выборе узла проекта, связанного с элементом
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tvStory_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node != null && e.Node.Tag is Control control)
            {
                // если выбор не пуст, то передаем элемент сетке свойств
                pgStoryElement.SelectedObject = control;
                storyPad.Select(control);
            }
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
            storyPad.Invalidate();
        }

        private void storyPad_OnSelected(object sender, RibbonSelectedEventArgs e)
        {
            var single = e.Selected.FirstOrDefault();
            pgStoryElement.SelectedObject = single;
            if (single != null)
                tvStory.SelectedNode = tvStory.Nodes.Cast<TreeNode>().FirstOrDefault(node => node.Tag == single);
            else
                tvStory.SelectedNode = null;
            pgStoryElement.SelectedObjects = e.Selected.ToArray();
        }

        private void storyPad_OnChanged(object sender, EventArgs e)
        {
            ContentChanged = true;
        }

        private void contextMenuStripPropertyGrid_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            GridItem item = pgStoryElement.SelectedGridItem;
            if (pgStoryElement.SelectedObjects == null || pgStoryElement.SelectedObjects.Length <= 1)
                resetToolStripMenuItem.Enabled = item.PropertyDescriptor.CanResetValue(pgStoryElement.SelectedObject);
            else
                resetToolStripMenuItem.Enabled = false;
        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GridItem item = pgStoryElement.SelectedGridItem;
            if (pgStoryElement.SelectedObjects == null || pgStoryElement.SelectedObjects.Length <= 1 &&
                item.PropertyDescriptor.CanResetValue(pgStoryElement.SelectedObject))
            {
                pgStoryElement.ResetSelectedProperty();
                item.Select();
                ContentChanged = true;
                storyPad.Invalidate();
            }
        }
    }
}
