using System.Text.Json;
using BlazorApp.Shared;
using Microsoft.JSInterop;

public class StateContainer
{
  private IJSRuntime jsRuntime;

  public StateContainer(IJSRuntime jsRuntime)
  {
    this.jsRuntime = jsRuntime;
  }

  private void SaveLinkBundleToLocalStorage()
  {
    var json = JsonSerializer.Serialize(LinkBundle);
    jsRuntime.InvokeVoidAsync("localStorage.setItem", "linkBundle", json);
  }

  public async Task LoadLinkBundleFromLocalStorage()
  {
    var json = await jsRuntime.InvokeAsync<string>("localStorage.getItem", "linkBundle");
    if (json != null)
    {
      LinkBundle = JsonSerializer.Deserialize<LinkBundle>(json) ?? new LinkBundle();
    }
  }

  private LinkBundle savedLinkBundle = new LinkBundle();

  public LinkBundle LinkBundle
  {
    get => savedLinkBundle ??= new LinkBundle();
    set
    {
      savedLinkBundle = value;
      NotifyStateChanged();
      SaveLinkBundleToLocalStorage();
    }
  }

  private User? user;

  public User? User
  {
    get => user;
    set
    {
      user = value;
      NotifyStateChanged();
    }
  }

  public void DeleteLinkFromBundle(Link link)
  {
    LinkBundle.Links.Remove(link);
    NotifyStateChanged();
    SaveLinkBundleToLocalStorage();
  }

  public void AddLinkToBundle(Link link)
  {
    LinkBundle.Links.Add(link);
    NotifyStateChanged();
    SaveLinkBundleToLocalStorage();
  }

  public void UpdateLinkInBundle(Link link, Link updatedLink)
  {
    link.Title = updatedLink.Title;
    link.Description = updatedLink.Description;
    link.Image = updatedLink.Image;

    NotifyStateChanged();
    SaveLinkBundleToLocalStorage();
  }

  public void ReorderLinks(int moveFromIndex, int moveToIndex)
  {
    var links = LinkBundle.Links;
    var itemToMove = links[moveFromIndex];
    links.RemoveAt(moveFromIndex);

    if (moveToIndex < links.Count)
    {
      links.Insert(moveToIndex, itemToMove);
    }
    else
    {
      links.Add(itemToMove);
    }

    NotifyStateChanged();
    SaveLinkBundleToLocalStorage();
  }

  public event Action? OnChange;

  private void NotifyStateChanged() => OnChange?.Invoke();
}