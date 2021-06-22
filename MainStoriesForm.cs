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

        private void MainStoriesForm_Load(object sender, EventArgs e)
        {
            tvLibrary.Nodes.Clear();
            foreach (var type in StoryLibrary.GetControlTypes())
            {
                var names = $"{type}".Split('.');
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

        private void tvLibrary_MouseMove(object sender, MouseEventArgs e)
        {
            if (tvLibrary.SelectedNode != null && isHold &&
                (Math.Abs(downPressed.X - e.Location.X) > 5 || Math.Abs(downPressed.Y - e.Location.Y) > 5))
            {
                tvLibrary.DoDragDrop(tvLibrary.SelectedNode, DragDropEffects.Move);
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

                element.Click += Element_Click;

                var controlNode = new TreeNode(element.Text) { Tag = element };
                tvStory.Nodes.Add(controlNode);
                tvStory.SelectedNode = controlNode;
            }

        }

        private void Element_Click(object sender, EventArgs e)
        {
            tcSelector.SelectedTab = tabPage1;
            tvStory.SelectedNode = tvStory.Nodes.Cast<TreeNode>().FirstOrDefault(item => item.Tag == sender);
            pgStoryElement.SelectedObject = sender;
        }

        private void tvStory_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node != null)
                pgStoryElement.SelectedObject = e.Node.Tag;
            else
                pgStoryElement.SelectedObject = null;
        }
    }
}
