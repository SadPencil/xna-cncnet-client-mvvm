using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

public sealed class CovariantReadOnlyObservableCollectionAdapter<TSource, TTarget> : IDisposable
    where TSource : TTarget
{
    private readonly ObservableCollection<TSource> _source;
    private readonly ObservableCollection<TTarget> _shadow;
    private readonly ReadOnlyObservableCollection<TTarget> _readOnlyShadow;

    /// <summary>
    /// Creates a new adapter with an empty internal collection.
    /// </summary>
    public CovariantReadOnlyObservableCollectionAdapter()
        : this(new ObservableCollection<TSource>())
    {
    }

    /// <summary>
    /// Creates a new adapter wrapping an existing collection.
    /// </summary>
    public CovariantReadOnlyObservableCollectionAdapter(ObservableCollection<TSource> source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        _source = source;
        _shadow = new ObservableCollection<TTarget>();
        _readOnlyShadow = new ReadOnlyObservableCollection<TTarget>(_shadow);

        foreach (var item in _source)
            _shadow.Add(item);

        _source.CollectionChanged += Source_CollectionChanged;
    }

    public ObservableCollection<TSource> Source => _source;

    public ReadOnlyObservableCollection<TTarget> Target => _readOnlyShadow;

    private void Source_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                for (int i = 0; i < e.NewItems.Count; i++)
                    _shadow.Insert(e.NewStartingIndex + i, (TTarget)e.NewItems[i]!);
                break;

            case NotifyCollectionChangedAction.Remove:
                for (int i = 0; i < e.OldItems.Count; i++)
                    _shadow.RemoveAt(e.OldStartingIndex);
                break;

            case NotifyCollectionChangedAction.Replace:
                for (int i = 0; i < e.NewItems.Count; i++)
                    _shadow[e.OldStartingIndex + i] = (TTarget)e.NewItems[i]!;
                break;

            case NotifyCollectionChangedAction.Move:
                _shadow.Move(e.OldStartingIndex, e.NewStartingIndex);
                break;

            case NotifyCollectionChangedAction.Reset:
                _shadow.Clear();
                foreach (var item in _source)
                    _shadow.Add(item);
                break;
        }
    }

    public void Dispose()
    {
        _source.CollectionChanged -= Source_CollectionChanged;
    }
}