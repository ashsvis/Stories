using Stories.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Stories
{
    public partial class MainStoriesForm : Form
    {
        public MainStoriesForm()
        {
            InitializeComponent();
            StoryLibrary.Init();
        }

        /// <summary>
        /// При первой загрузке заполняем дерево библиотеки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainStoriesForm_Load(object sender, EventArgs e)
        {
            tvLibrary.Nodes.Clear();
            foreach (var type in StoryLibrary.GetControlTypes())
            {
                // имя типа составное и содержит имена namespace
                var names = $"{type}".Split('.');
                // поэтому забираем только крайнее правое, имя типа
                tvLibrary.Nodes.Add(new TreeNode(names[names.Length - 1]) { Tag = type });
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
                // прицепляем клик на вновь поставленный элемент
                element.Click += Element_Click;
                // добавляем новый элемент в дерево проекта
                var controlNode = new TreeNode(element.Text) { Tag = element };
                tvStory.Nodes.Add(controlNode);
                // делаем его текущим
                tvStory.SelectedNode = controlNode;
            }

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
    }
}
