using GLib;
using Gtk;
using Application = Gtk.Application;
using UI = Gtk.Builder.ObjectAttribute;

namespace GtkTreeView
{
    class MainWindow : Window
    {
        [UI] private TreeView MyTree = null;
        [UI] private TreeStore MyStore = null;
        [UI] private CellRendererToggle MyToggler = null;
        [UI] private TreeViewColumn NameColumn = null;

        enum StoreColumn
        {
            Name = 0,
            CheckState = 1
        }
        enum CheckState
        {
            False = 0,
            True = 1,
            Inconsistent = 2
        }
        
        public MainWindow() : this(new Builder("MainWindow.glade"))
        {
        }

        private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
        {
            builder.Autoconnect(this);

            var it1 = MyStore.AppendValues("Europe", (int)CheckState.False);
            var it2 = MyStore.AppendValues(it1, "England", (int)CheckState.False);
            var it3 = MyStore.AppendValues(it1, "France", (int)CheckState.False);
            var it4 = MyStore.AppendValues(it2, "London", (int)CheckState.False);
            var it5 = MyStore.AppendValues(it2, "Leicester", (int)CheckState.False);
            var it6 = MyStore.AppendValues(it3, "Paris", (int)CheckState.False);
            var it7 = MyStore.AppendValues(it3, "Nice", (int)CheckState.False);
            
            NameColumn.SetCellDataFunc(MyToggler, ToggleStateFunction);
            MyTree.ExpandAll();
            MyToggler.Toggled += MyTogglerOnToggled;
            DeleteEvent += Window_DeleteEvent;
        }

        private void ToggleStateFunction(TreeViewColumn tree_column, CellRenderer cell,
            ITreeModel tree_model, TreeIter iter)
        {
            var checkState = (CheckState)MyStore.GetValue(iter, (int)StoreColumn.CheckState);
            cell.SetProperty("active", new Value(checkState== CheckState.True));
            cell.SetProperty("inconsistent", new Value(checkState==CheckState.Inconsistent));
        }

        private void MyTogglerOnToggled(object o, ToggledArgs args)
        {
            MyStore.GetIter(out var it, new TreePath(args.Path));

            var checkState = (CheckState)MyStore.GetValue(it, (int)StoreColumn.CheckState);
            var newCheckState = checkState == CheckState.True ? CheckState.False : CheckState.True;
            MyStore.SetValue(it, (int)StoreColumn.CheckState, (int)newCheckState);

            //Propagate down
            AssimilateDescendantsCheckState(it, newCheckState);
            
            //Propagate up
            while(MyStore.IterParent(out var parent, it))
            {
                var s = CalculateCheckState(parent);
                MyStore.SetValue(parent, (int)StoreColumn.CheckState, (int)s);
                it = parent;
            }
        }

        void AssimilateDescendantsCheckState(TreeIter it, CheckState state)
        {
            if (MyStore.IterHasChild(it))
            {
                MyStore.IterChildren(out var child, it);
                do
                {
                    var childState = (CheckState)MyStore.GetValue(child, (int)StoreColumn.CheckState);
                    if (childState != state)
                    {
                        MyStore.SetValue(child, (int)StoreColumn.CheckState, (int)state);
                        AssimilateDescendantsCheckState(child, state);
                    }
                }while (MyStore.IterNext(ref child));
            }
        }
        
        CheckState CalculateCheckState(TreeIter it)
        {
            var num0 = 0;
            var num1 = 0;
            
            MyStore.IterChildren(out var child, it);
            do
            {
                var checkState = (CheckState)MyStore.GetValue(child, (int)StoreColumn.CheckState);
                switch (checkState)
                {
                    case CheckState.False:
                        num0++;
                        break;
                    case CheckState.True:
                        num1++;
                        break;
                    case CheckState.Inconsistent:
                        return CheckState.Inconsistent;
                }

                if (num0 > 0 && num1 > 0)
                    return CheckState.Inconsistent;
            }while (MyStore.IterNext(ref child));

            return num1 > 0? CheckState.True : CheckState.False;
        }

        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            Application.Quit();
        }
    }
}
