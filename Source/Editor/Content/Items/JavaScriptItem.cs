// Copyright (c) Wojciech Figat. All rights reserved.

using FlaxEngine;

namespace FlaxEditor.Content
{
    /// <summary>
    /// Content item that contains a Java gameplay script.
    /// </summary>
    /// <seealso cref="FlaxEditor.Content.ScriptItem" />
    public sealed class JavaScriptItem : ScriptItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JavaScriptItem"/> class.
        /// </summary>
        /// <param name="path">The path to the item.</param>
        public JavaScriptItem(string path)
        : base(path)
        {
        }

        /// <inheritdoc />
        public override string TypeDescription => "Java Source Code";

        /// <inheritdoc />
        public override SpriteHandle DefaultThumbnail => Editor.Instance.Icons.Document128;
    }
}
