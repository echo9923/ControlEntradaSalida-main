using System;
using System.Drawing;
using System.Windows.Forms;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 网页浏览窗体，支持完整的网页浏览和交互功能
    /// </summary>
    public partial class WebBrowserForm : Form
    {
        private WebBrowser webBrowser;
        private ToolStrip toolStrip;
        private ToolStripButton btnBack;
        private ToolStripButton btnForward;
        private ToolStripButton btnRefresh;
        private ToolStripButton btnHome;
        private ToolStripTextBox txtUrl;
        private ToolStripButton btnGo;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripProgressBar progressBar;

        public WebBrowserForm()
        {
            InitializeComponent();
            InitializeWebBrowser();
            LoadHomePage();
        }

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponent()
        {
            this.Text = "网页浏览器 - 哔哩哔哩";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Icon = SystemIcons.Application;
            this.MinimumSize = new Size(800, 600);

            // 创建工具栏
            CreateToolStrip();
            
            // 创建WebBrowser控件
            CreateWebBrowserControl();
            
            // 创建状态栏
            CreateStatusStrip();
        }

        /// <summary>
        /// 创建工具栏
        /// </summary>
        private void CreateToolStrip()
        {
            toolStrip = new ToolStrip();
            toolStrip.ImageScalingSize = new Size(24, 24);
            toolStrip.Dock = DockStyle.Top;

            // 后退按钮
            btnBack = new ToolStripButton();
            btnBack.Text = "后退";
            btnBack.Enabled = false;
            btnBack.Click += BtnBack_Click;

            // 前进按钮
            btnForward = new ToolStripButton();
            btnForward.Text = "前进";
            btnForward.Enabled = false;
            btnForward.Click += BtnForward_Click;

            // 刷新按钮
            btnRefresh = new ToolStripButton();
            btnRefresh.Text = "刷新";
            btnRefresh.Click += BtnRefresh_Click;

            // 主页按钮
            btnHome = new ToolStripButton();
            btnHome.Text = "主页";
            btnHome.Click += BtnHome_Click;

            // 分隔符
            ToolStripSeparator separator1 = new ToolStripSeparator();

            // 地址栏
            txtUrl = new ToolStripTextBox();
            txtUrl.Size = new Size(400, 25);
            txtUrl.KeyDown += TxtUrl_KeyDown;

            // 转到按钮
            btnGo = new ToolStripButton();
            btnGo.Text = "转到";
            btnGo.Click += BtnGo_Click;

            // 添加控件到工具栏
            toolStrip.Items.AddRange(new ToolStripItem[]
            {
                btnBack,
                btnForward,
                btnRefresh,
                btnHome,
                separator1,
                new ToolStripLabel("地址:"),
                txtUrl,
                btnGo
            });

            this.Controls.Add(toolStrip);
        }

        /// <summary>
        /// 创建WebBrowser控件
        /// </summary>
        private void CreateWebBrowserControl()
        {
            webBrowser = new WebBrowser();
            webBrowser.Dock = DockStyle.Fill;
            webBrowser.ScriptErrorsSuppressed = true;
            webBrowser.AllowWebBrowserDrop = true;
            webBrowser.WebBrowserShortcutsEnabled = true;
            
            // 绑定事件
            webBrowser.DocumentCompleted += WebBrowser_DocumentCompleted;
            webBrowser.Navigating += WebBrowser_Navigating;
            webBrowser.ProgressChanged += WebBrowser_ProgressChanged;
            webBrowser.CanGoBackChanged += WebBrowser_CanGoBackChanged;
            webBrowser.CanGoForwardChanged += WebBrowser_CanGoForwardChanged;

            this.Controls.Add(webBrowser);
        }

        /// <summary>
        /// 创建状态栏
        /// </summary>
        private void CreateStatusStrip()
        {
            statusStrip = new StatusStrip();

            statusLabel = new ToolStripStatusLabel();
            statusLabel.Text = "就绪";
            statusLabel.Spring = true;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;

            progressBar = new ToolStripProgressBar();
            progressBar.Size = new Size(200, 16);
            progressBar.Visible = false;

            statusStrip.Items.AddRange(new ToolStripItem[]
            {
                statusLabel,
                progressBar
            });

            this.Controls.Add(statusStrip);
        }

        /// <summary>
        /// 初始化WebBrowser设置
        /// </summary>
        private void InitializeWebBrowser()
        {
            // 设置浏览器兼容性
            SetBrowserFeatureControl();
        }

        /// <summary>
        /// 设置浏览器功能控制，提高兼容性
        /// </summary>
        private void SetBrowserFeatureControl()
        {
            try
            {
                // 设置浏览器模拟版本，使用IE11
                var appName = System.IO.Path.GetFileName(Application.ExecutablePath);
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION",
                    appName, 11001, Microsoft.Win32.RegistryValueKind.DWord);
            }
            catch
            {
                // 忽略注册表设置失败的情况
            }
        }

        /// <summary>
        /// 加载主页
        /// </summary>
        private void LoadHomePage()
        {
            NavigateToUrl("https://www.bilibili.com/");
        }

        /// <summary>
        /// 导航到指定URL
        /// </summary>
        /// <param name="url">目标URL</param>
        private void NavigateToUrl(string url)
        {
            try
            {
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "https://" + url;
                }

                webBrowser.Navigate(url);
                txtUrl.Text = url;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导航失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region 事件处理器

        private void BtnBack_Click(object sender, EventArgs e)
        {
            if (webBrowser.CanGoBack)
            {
                webBrowser.GoBack();
            }
        }

        private void BtnForward_Click(object sender, EventArgs e)
        {
            if (webBrowser.CanGoForward)
            {
                webBrowser.GoForward();
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            webBrowser.Refresh();
        }

        private void BtnHome_Click(object sender, EventArgs e)
        {
            LoadHomePage();
        }

        private void BtnGo_Click(object sender, EventArgs e)
        {
            NavigateToUrl(txtUrl.Text);
        }

        private void TxtUrl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                NavigateToUrl(txtUrl.Text);
            }
        }

        private void WebBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (webBrowser.ReadyState == WebBrowserReadyState.Complete)
            {
                statusLabel.Text = "页面加载完成";
                progressBar.Visible = false;
                
                // 更新地址栏
                if (webBrowser.Url != null)
                {
                    txtUrl.Text = webBrowser.Url.ToString();
                }

                // 更新窗体标题
                if (!string.IsNullOrEmpty(webBrowser.DocumentTitle))
                {
                    this.Text = $"{webBrowser.DocumentTitle} - 网页浏览器";
                }
            }
        }

        private void WebBrowser_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            statusLabel.Text = $"正在导航到: {e.Url}";
            progressBar.Visible = true;
            progressBar.Value = 0;
        }

        private void WebBrowser_ProgressChanged(object sender, WebBrowserProgressChangedEventArgs e)
        {
            if (e.MaximumProgress > 0)
            {
                var progress = (int)((e.CurrentProgress * 100) / e.MaximumProgress);
                progressBar.Value = Math.Min(progress, 100);
                statusLabel.Text = $"加载中... {progress}%";
            }
        }

        private void WebBrowser_CanGoBackChanged(object sender, EventArgs e)
        {
            btnBack.Enabled = webBrowser.CanGoBack;
        }

        private void WebBrowser_CanGoForwardChanged(object sender, EventArgs e)
        {
            btnForward.Enabled = webBrowser.CanGoForward;
        }

        #endregion

        /// <summary>
        /// 窗体关闭时的清理
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // 清理WebBrowser资源
            if (webBrowser != null)
            {
                webBrowser.Dispose();
            }

            base.OnFormClosed(e);
        }
    }
}