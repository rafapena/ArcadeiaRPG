using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Code.UI.Lists
{
    interface IToolCollectionFrameOperations
    {
        void SelectTabSuccess();

        void SelectTabFailed();

        void SelectToolSuccess();

        void SelectToolFailed();

        void UndoSelectToolSuccess();
    }
}