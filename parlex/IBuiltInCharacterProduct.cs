using System;

namespace parlex {
    interface IBuiltInCharacterProduct {
        bool Match(Int32 codePoint);
    }
}
