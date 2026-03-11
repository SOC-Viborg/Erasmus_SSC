using Erasmus_SSC.Client.Dtos;
using Erasmus_SSC.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;

namespace Erasmus_SSC.Client.Pages;

public partial class Admin
{

    private bool _loading = true;
    private bool _busy = false;
    private string? _error;

    private bool showAddUserForm = false;
    private AdminCreateUserDto newUser = new();

    public List<AdminUserDto> Users { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        _error = null;
        _loading = true;

        var state = await AuthProvider.GetAuthenticationStateAsync();
        var principal = state.User;

        if (principal.Identity?.IsAuthenticated != true)
        {
            Nav.NavigateTo("/login");
            return;
        }

        if (!principal.IsInRole("Admin"))
        {
            _error = "Access denied: Admin role required.";
            Nav.NavigateTo("/");
            return;
        }

        await ReloadUsersAsync();
        await ReloadNewsAsync();
    }


    private async Task ReloadUsersAsync()
    {
        _error = null;
        _loading = true;

        try
        {
            Users = await AdminApi.GetUsersAsync();
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _loading = false;
        }
    }

    private void ShowAddUser()
    {
        _error = null;
        showAddUserForm = true;
    }

    private void CancelAddUser()
    {
        showAddUserForm = false;
        newUser = new AdminCreateUserDto();
    }

    private async Task SaveUserAsync()
    {
        _error = null;
        _busy = true;

        try
        {
            var created = await AdminApi.CreateUserAsync(new AdminCreateUserDto
            {
                UserName = newUser.UserName.Trim(),
                Email = newUser.Email.Trim(),
                Password = newUser.Password
            });

            Users.Add(created);
            showAddUserForm = false;
            newUser = new AdminCreateUserDto();
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }
    // Edit User state
    private bool _showEditUser;
    private int _editUserId;
    private EditUserModel _editUser = new();

    private sealed class EditUserModel
    {
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Password { get; set; }
    }

    private void StartEditUser(AdminUserDto user)
    {
        _editUserId = user.Id;
        _editUser = new EditUserModel
        {
            UserName = user.UserName ?? "",
            Email = user.Email ?? ""
        };
        _showEditUser = true;
    }

    private void CancelEditUser()
    {
        _showEditUser = false;
        _editUserId = 0;
        _editUser = new EditUserModel();
    }

    private async Task SaveEditUserAsync()
    {
        if (_busy) return;

        _busy = true;
        try
        {
            var dto = new AdminUpdateUserDto
            {
                UserName = _editUser.UserName,
                Email = _editUser.Email,
                Password = string.IsNullOrWhiteSpace(_editUser.Password) ? null : _editUser.Password
            };

            var updated = await AdminApi.UpdateUserAsync(_editUserId, dto);

            // Update list in-place
            var idx = Users.FindIndex(u => u.Id == updated.Id);
            if (idx >= 0) Users[idx] = updated;

            CancelEditUser();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task DeleteUserAsync(int id)
    {
        _error = null;
        _busy = true;

        try
        {
            await AdminApi.DeleteUserAsync(id);
            Users.RemoveAll(u => u.Id == id);
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }
    private bool _newsLoading = true;
    private bool _newsBusy = false;
    private string? _newsError;

    private bool showAddNewsForm = false;
    private NewsCreateModel newNews = new();
    private IBrowserFile? newNewsImage;

    public List<Erasmus_SSC.Client.Dtos.AdminNewsDto> NewsItems { get; set; } = new();

    private sealed class NewsCreateModel
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime Date { get; set; } = DateTime.UtcNow.Date;
    }

    private void ShowAddNews()
    {
        _newsError = null;
        showAddNewsForm = true;
    }

    private void CancelAddNews()
    {
        showAddNewsForm = false;
        newNews = new NewsCreateModel();
        newNewsImage = null;
    }

    private void OnNewsImageSelected(InputFileChangeEventArgs e)
    {
        newNewsImage = e.FileCount > 0 ? e.File : null;
    }

    private async Task ReloadNewsAsync()
    {
        _newsError = null;
        _newsLoading = true;
        try
        {
            NewsItems = await NewsApi.GetNewsAsync();
        }
        catch (Exception ex)
        {
            _newsError = ex.Message;
        }
        finally
        {
            _newsLoading = false;
        }
    }

    private async Task SaveNewsAsync()
    {
        _newsError = null;
        _newsBusy = true;

        try
        {
            if (string.IsNullOrWhiteSpace(newNews.Title) || string.IsNullOrWhiteSpace(newNews.Description))
            {
                _newsError = "Title and Description are required.";
                return;
            }


            var description = newNews.Description.Replace("\r\n", "\n").Replace("\n", "<br>");

            var created = await NewsApi.CreateNewsAsync(
                newNews.Title.Trim(),
                description,
                newNews.Date,
                newNewsImage
            );

            NewsItems.Insert(0, created);
            CancelAddNews();
        }
        catch (Exception ex)
        {
            _newsError = ex.Message;
        }
        finally
        {
            _newsBusy = false;
        }
    }

    // Edit News state
    private bool _showEditNews;
    private int _editNewsId;
    private NewsEditModel _editNews = new();
    private IBrowserFile? _editNewsImage;
    private string? _editNewsExistingImageUrl;

    private sealed class NewsEditModel
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime Date { get; set; } = DateTime.UtcNow.Date;
    }

    private void StartEditNews(AdminNewsDto news)
    {
        _newsError = null;

        _editNewsId = news.Id;
        _editNewsExistingImageUrl = news.ImageUrl;

        _editNews = new NewsEditModel
        {
            Title = news.Title ?? "",
            Date = news.Date,

            Description = (news.Description ?? "").Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<br />", "\n")
        };

        _editNewsImage = null;
        _showEditNews = true;
    }

    private void CancelEditNews()
    {
        _showEditNews = false;
        _editNewsId = 0;
        _editNews = new NewsEditModel();
        _editNewsImage = null;
        _editNewsExistingImageUrl = null;
    }

    private void OnEditNewsImageSelected(InputFileChangeEventArgs e)
    {
        _editNewsImage = e.FileCount > 0 ? e.File : null;
    }

    private async Task SaveEditNewsAsync()
    {
        if (_newsBusy) return;

        _newsError = null;
        _newsBusy = true;

        try
        {
            if (string.IsNullOrWhiteSpace(_editNews.Title) || string.IsNullOrWhiteSpace(_editNews.Description))
            {
                _newsError = "Title and Description are required.";
                return;
            }

            var description = _editNews.Description.Replace("\r\n", "\n").Replace("\n", "<br>");

            var updated = await NewsApi.UpdateNewsAsync(
                _editNewsId,
                _editNews.Title.Trim(),
                description,
                _editNews.Date,
                _editNewsImage
            );

            var idx = NewsItems.FindIndex(x => x.Id == updated.Id);
            if (idx >= 0) NewsItems[idx] = updated;

            CancelEditNews();
        }
        catch (Exception ex)
        {
            _newsError = ex.Message;
        }
        finally
        {
            _newsBusy = false;
        }
    }


    private async Task DeleteNewsAsync(int id)
    {
        _newsError = null;
        _newsBusy = true;

        try
        {
            await NewsApi.DeleteNewsAsync(id);
            NewsItems.RemoveAll(x => x.Id == id);
        }
        catch (Exception ex)
        {
            _newsError = ex.Message;
        }
        finally
        {
            _newsBusy = false;
        }
    }

}



