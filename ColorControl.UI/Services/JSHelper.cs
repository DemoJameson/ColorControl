﻿using Microsoft.JSInterop;

namespace ColorControl.UI.Services;

public class RectDimension
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

public class JSHelper(IJSRuntime jsRuntime)
{
    private IJSObjectReference? jsModule;

    public async Task<bool> IsFormValid(string id)
    {
        return await InvokeAsync<bool>("FormValid", id);
    }

    public async Task OpenModal(string id)
    {
        await InvokeVoidAsync("OpenModal", id);
    }

    public async Task CloseModal(string id)
    {
        await InvokeVoidAsync("CloseModal", id);
    }

    public async Task ShowToast(string id)
    {
        await InvokeVoidAsync("ShowToast", id);
    }

    public async Task NavigateTo(string uri, int timeout = 0)
    {
        await InvokeVoidAsync("NavigateTo", uri, timeout);
    }

    public async Task<RectDimension> GetElementDimensions(string id)
    {
        jsModule = await LoadModule();

        return await jsModule.InvokeAsync<RectDimension>("GetElementDimensions", id);
    }

    public async Task<string> GetElementClassName(string id)
    {
        jsModule = await LoadModule();

        return await jsModule.InvokeAsync<string>("GetElementClassName", id);
    }

    private async ValueTask<T> InvokeAsync<T>(string identifier, params object?[]? names) where T : struct
    {
        jsModule = await LoadModule();

        return await jsModule.InvokeAsync<T>(identifier, names);
    }

    private async ValueTask InvokeVoidAsync(string identifier, params object?[]? names)
    {
        jsModule = await LoadModule();

        await jsModule.InvokeVoidAsync(identifier, names);
    }

    private async Task<IJSObjectReference> LoadModule()
    {
        return jsModule ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./helper.js");
    }
}