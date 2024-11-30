using System;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

using Jelly;

namespace Beebo.Graphics;

public class RendererList : ICollection<ExtendedRenderer>, IEnumerable<ExtendedRenderer>, IEnumerable
{
    private readonly List<ExtendedRenderer> _renderers = [];
    private readonly HashSet<Tag> _presentTags = [];
    private bool _isDirty = true;

    public int Count => _renderers.Count;
    bool ICollection<ExtendedRenderer>.IsReadOnly => false;

    public static RendererList Shared { get; } = [];

    internal static void Initialize()
    {
        // SceneManager.ActiveSceneChanged += SceneBegin;
    }

    internal void SceneBegin(Scene scene)
    {
        foreach(var renderer in _renderers)
            if(renderer.Visible)
                renderer.SceneBegin(scene);
    }

    public void PreDraw()
    {
        foreach(var renderer in _renderers)
            if(renderer.Visible)
                renderer.PreDraw();
    }

    public void BeginDraw(GameTime gameTime)
    {
        foreach(var renderer in _renderers)
            if(renderer.Visible)
                renderer.BeginDraw(gameTime);
    }

    public void Draw(GameTime gameTime)
    {
        foreach(var renderer in _renderers)
            if(renderer.Visible)
                renderer.Draw(gameTime);
    }

    public void PostDraw(GameTime gameTime)
    {
        foreach(var renderer in _renderers)
            if(renderer.Visible)
                renderer.PostDraw(gameTime);
    }

    public void DrawUI(GameTime gameTime)
    {
        foreach(var renderer in _renderers)
            if(renderer.Visible)
                renderer.DrawUI(gameTime);
    }

    public void DrawDebug(GameTime gameTime)
    {
        foreach(var renderer in _renderers)
            if(renderer.Visible)
                renderer.DrawDebug(gameTime);
    }

    public void DrawDebugUI(GameTime gameTime)
    {
        foreach(var renderer in _renderers)
            if(renderer.Visible)
                renderer.DrawDebugUI(gameTime);
    }

    private void CheckDirty(bool triggerEvents = false)
    {
        if(!_isDirty) return;

        _presentTags.Clear();
        int i = 0;
        foreach(var renderer in _renderers)
        {
            if(triggerEvents)
                renderer.DrawOrder = i++;

            _presentTags.Add(renderer.Tag);
        }

        _isDirty = false;
    }

    public void Draw(GameTime gameTime, Tag tag, TagFilter tagFilter)
    {
        CheckDirty(true);

        if(tagFilter != TagFilter.None && tagFilter != TagFilter.NoFiltering && !_presentTags.Contains(tag))
            return;

        foreach(var renderer in _renderers)
        {
            if(renderer.Tag.Matches(tag, tagFilter))
            {
                renderer.Draw(gameTime);
            }
        }
    }

    void ICollection<ExtendedRenderer>.Add(ExtendedRenderer item)
    {
        if(!this.Add(item))
        {
            throw new InvalidOperationException($"The element is already present");
        }
    }

    /// <summary>
    /// Adds an item to the <see cref="ICollection"/>
    /// </summary>
    /// <param name="item">The object to add to the <see cref="ICollection"/></param>
    /// <returns>true if the element is added to the HashSet object; false if the element is already present.</returns>
    public bool Add(ExtendedRenderer item)
    {
        bool add()
        {
            _isDirty = true;
            _renderers.Add(item);
            item._drawOrder = _renderers.Count - 1;
            return true;
        }

        return !_renderers.Contains(item) && add();
    }

    public void Clear()
    {
        _renderers.Clear();
        _isDirty = true;
    }

    public bool Contains(ExtendedRenderer item)
    {
        return _renderers.Contains(item);
    }

    public bool Contains(Tag tag, TagFilter tagFilter)
    {
        if(!_isDirty && tagFilter != TagFilter.None && tagFilter != TagFilter.NoFiltering)
        {
            return _presentTags.Contains(tag);
        }

        foreach(var renderer in _renderers)
            if(renderer.Tag.Matches(tag, tagFilter))
                return true;

        return false;
    }

    public int AmountOf<T>() where T : ExtendedRenderer
    {
        int count = 0;
        foreach(var renderer in _renderers)
            if(renderer is T)
                count++;

        return count;
    }

    public int AmountOf(Tag tag, TagFilter tagFilter)
    {
        int count = 0;
        foreach(var renderer in _renderers)
            if(renderer.Tag.Matches(tag, tagFilter))
                count++;

        return count;
    }

    public void CopyTo(ExtendedRenderer[] array, int arrayIndex)
    {
        _renderers.CopyTo(array, arrayIndex);
    }

    public bool Remove(ExtendedRenderer item)
    {
        if(_renderers.Remove(item))
        {
            _isDirty = true;
            return true;
        }
        return false;
    }

    public bool Remove(IEnumerable<ExtendedRenderer> toRemove)
    {
        bool result = false;
        foreach (var renderer in toRemove)
            result |= Remove(renderer);
        return result;
    }

    public bool RemoveAll(Tag tag, TagFilter tagFilter)
    {
        HashSet<ExtendedRenderer> toRemove = [];

        foreach(var renderer in _renderers)
            if(renderer.Tag.Matches(tag, tagFilter))
                toRemove.Add(renderer);

        if(toRemove.Count == 0)
            return false;

        return Remove(toRemove);
    }

    public bool RemoveFirst(Tag tag, TagFilter tagFilter)
    {
        ExtendedRenderer toRemove = null;

        foreach(var renderer in _renderers)
        {
            if(renderer.Tag.Matches(tag, tagFilter))
            {
                toRemove = renderer;
                break;
            }
        }

        if(toRemove is null)
            return false;

        return Remove(toRemove);
    }

    public bool RemoveLast(Tag tag, TagFilter tagFilter)
    {
        ExtendedRenderer toRemove = null;

        for(int i = _renderers.Count - 1; i >= 0; i--)
        {
            if(_renderers[i].Tag.Matches(tag, tagFilter))
            {
                toRemove = _renderers[i];
                break;
            }
        }

        if(toRemove is null)
            return false;

        return Remove(toRemove);
    }

    public IEnumerator<ExtendedRenderer> GetEnumerator()
    {
        return _renderers.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
