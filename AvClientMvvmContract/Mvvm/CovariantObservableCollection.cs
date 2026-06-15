using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace AvClientMvvmContract.Mvvm;

public sealed class CovariantObservableCollection<TSource, TTarget> : IDisposable
    where TSource : TTarget
{
    private readonly ObservableCollection<TSource> _source;
    private readonly ObservableCollection<TTarget> _target;
    private bool _isSyncing;

    /// <summary>
    /// Creates a new adapter with an empty internal collection.
    /// </summary>
    public CovariantObservableCollection()
        : this(new ObservableCollection<TSource>())
    {
    }

    /// <summary>
    /// Creates a new adapter wrapping an existing collection.
    /// </summary>
    public CovariantObservableCollection(ObservableCollection<TSource> source)
    {
        if (source == null)
            throw new ArgumentNullException("source");

        _source = source;
        _target = new ObservableCollection<TTarget>();

        foreach (var item in _source)
            _target.Add(item);

        _source.CollectionChanged += OnSourceChanged;
        _target.CollectionChanged += OnTargetChanged;
    }

    public ObservableCollection<TSource> Source => _source;
    public ObservableCollection<TTarget> Target => _target;

    private void OnSourceChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isSyncing) return;
        _isSyncing = true;
        try { ApplyToTarget(e); }
        finally { _isSyncing = false; }
    }

    private void OnTargetChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isSyncing) return;
        _isSyncing = true;
        try { ApplyToSource(e); }
        finally { _isSyncing = false; }
    }

    private void ApplyToTarget(NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                for (int i = 0; i < e.NewItems.Count; i++)
                    _target.Insert(e.NewStartingIndex + i, (TTarget)e.NewItems[i]!);
                break;

            case NotifyCollectionChangedAction.Remove:
                for (int i = 0; i < e.OldItems.Count; i++)
                    _target.RemoveAt(e.OldStartingIndex);
                break;

            case NotifyCollectionChangedAction.Replace:
                for (int i = 0; i < e.NewItems.Count; i++)
                    _target[e.OldStartingIndex + i] = (TTarget)e.NewItems[i]!;
                break;

            case NotifyCollectionChangedAction.Move:
                _target.Move(e.OldStartingIndex, e.NewStartingIndex);
                break;

            case NotifyCollectionChangedAction.Reset:
                _target.Clear();
                foreach (var item in _source)
                    _target.Add(item);
                break;
        }
    }

    private void ApplyToSource(NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                for (int i = 0; i < e.NewItems.Count; i++)
                {
                    if (e.NewItems[i] is TSource sourceItem)
                    {
                        _source.Insert(e.NewStartingIndex + i, sourceItem);
                    }
                    else
                    {
                        ThrowInvalidItemType(e.NewItems[i]);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                for (int i = 0; i < e.OldItems.Count; i++)
                {
                    if (e.OldItems[i] is TSource sourceItem)
                    {
                        _source.Remove(sourceItem);
                    }
                    else
                    {
                        ThrowInvalidItemType(e.OldItems[i]);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Replace:
                for (int i = 0; i < e.NewItems.Count; i++)
                {
                    if (e.NewItems[i] is TSource newSourceItem)
                    {
                        var oldItem = (TSource)e.OldItems[i]!;
                        int index = _source.IndexOf(oldItem);
                        if (index >= 0)
                        {
                            _source[index] = newSourceItem;
                        }
                    }
                    else
                    {
                        ThrowInvalidItemType(e.NewItems[i]);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Move:
                _source.Move(e.OldStartingIndex, e.NewStartingIndex);
                break;

            case NotifyCollectionChangedAction.Reset:
                _source.Clear();
                foreach (var item in _target)
                {
                    if (item is TSource sourceItem)
                    {
                        _source.Add(sourceItem);
                    }
                    else
                    {
                        ThrowInvalidItemType(item);
                    }
                }
                break;
        }
    }

    private static void ThrowInvalidItemType(object? item)
    {
        string itemType = item?.GetType().FullName ?? "null";
        throw new InvalidOperationException(
            $"Cannot synchronize item of type '{itemType}' because it cannot be cast to the source type '{typeof(TSource).FullName}'. " +
            "Only instances of types assignable to the source type may be added through the covariant view.");
    }

    public void Dispose()
    {
        _source.CollectionChanged -= OnSourceChanged;
        _target.CollectionChanged -= OnTargetChanged;
    }
}