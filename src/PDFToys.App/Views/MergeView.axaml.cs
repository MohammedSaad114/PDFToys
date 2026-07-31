using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PDFToys.App.ViewModels;
using System;

namespace PDFToys.App.Views;

public partial class MergeView : UserControl
{
    private Point _dragStartPosition;
    private PointerPressedEventArgs? _pointerPressedArgs;
    private MergeItem? _draggedItem;
    private int _draggedIndex = -1;

    public MergeView()
    {
        InitializeComponent();
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
        MergeItemsListBox.AddHandler(
            InputElement.PointerPressedEvent,
            OnListBoxPointerPressed,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
            handledEventsToo: true);
        MergeItemsListBox.AddHandler(
            InputElement.PointerMovedEvent,
            OnListBoxPointerMoved,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
            handledEventsToo: true);
    }

    private void OnListBoxPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _dragStartPosition = e.GetPosition(MergeItemsListBox);
        _pointerPressedArgs = e;
        var item = FindItemFromSource(e.Source);
        _draggedItem = item;
        _draggedIndex = item is null || DataContext is not MergeViewModel viewModel
            ? -1
            : viewModel.MergeItems.IndexOf(item);
    }

    private async void OnListBoxPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggedItem is null)
        {
            return;
        }

        if (!e.GetCurrentPoint(MergeItemsListBox).Properties.IsLeftButtonPressed)
        {
            _draggedItem = null;
            _draggedIndex = -1;
            return;
        }

        var currentPosition = e.GetPosition(MergeItemsListBox);
        var delta = currentPosition - _dragStartPosition;
        if (Math.Abs(delta.X) < 4 && Math.Abs(delta.Y) < 4)
        {
            return;
        }

        if (_pointerPressedArgs is null)
        {
            return;
        }

        var dataTransfer = new DataTransfer();
        dataTransfer.Add(DataTransferItem.CreateText(_draggedItem.FullPath));
        await DragDrop.DoDragDropAsync(_pointerPressedArgs, dataTransfer, DragDropEffects.Move);
        _draggedItem = null;
        _draggedIndex = -1;
        _pointerPressedArgs = null;
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        var dragText = e.DataTransfer.TryGetText();
        if (string.IsNullOrWhiteSpace(dragText))
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        e.DragEffects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MergeViewModel viewModel)
        {
            return;
        }

        var draggedPath = e.DataTransfer.TryGetText();
        if (string.IsNullOrWhiteSpace(draggedPath))
        {
            return;
        }

        if (_draggedIndex < 0 || _draggedIndex >= viewModel.MergeItems.Count)
        {
            return;
        }

        var newIndex = GetTargetIndex(e.GetPosition(MergeItemsListBox), viewModel);
        if (newIndex < 0 || newIndex >= viewModel.MergeItems.Count)
        {
            newIndex = viewModel.MergeItems.Count - 1;
        }

        if (newIndex != _draggedIndex)
        {
            viewModel.ReorderItem(_draggedIndex, newIndex);
        }

        e.Handled = true;
    }

    private int GetTargetIndex(Point position, MergeViewModel viewModel)
    {
        var targetItem = FindItemAtPosition(position);
        if (targetItem is null)
        {
            return viewModel.MergeItems.Count - 1;
        }

        var index = viewModel.MergeItems.IndexOf(targetItem);
        return index < 0 ? viewModel.MergeItems.Count - 1 : index;
    }

    private MergeItem? FindItemAtPosition(Point position)
    {
        var element = MergeItemsListBox.InputHitTest(position) as Control;
        return FindItemFromSource(element);
    }

    private static MergeItem? FindItemFromSource(object? source)
    {
        var control = source as Control;
        while (control is not null)
        {
            if (control is ListBoxItem item && item.DataContext is MergeItem mergeItem)
            {
                return mergeItem;
            }

            control = control.Parent as Control;
        }

        return null;
    }
}
