import { useState, useEffect, useCallback } from "react";
import userApi from "../api/userApi";
import feedbackApi from "../api/feedbackApi";

const ReportsPage = () => {
  const [summaryData, setSummaryData] = useState(null);
  const [staffData, setStaffData] = useState([]);
  const [trendData, setTrendData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [filtering, setFiltering] = useState(false);
  const [error, setError] = useState("");

  // Report filters
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [categoryFilter, setCategoryFilter] = useState("");
  const [appliedFilters, setAppliedFilters] = useState({
    fromDate: "",
    toDate: "",
    categoryId: "",
  });
  const [categories, setCategories] = useState([]);

  const fetchReportData = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const params = {};
      if (appliedFilters.fromDate) params.fromDate = new Date(appliedFilters.fromDate).toISOString();
      if (appliedFilters.toDate) params.toDate = new Date(appliedFilters.toDate).toISOString();
      if (appliedFilters.categoryId) params.categoryId = appliedFilters.categoryId;

      const [summaryRes, staffRes, trendRes] = await Promise.all([
        userApi.getReports(params).catch((err) => {
          console.error(err);
          throw new Error("Không thể tải báo cáo tổng quan.");
        }),
        userApi.getStaffWorkload(params).catch((err) => {
          console.error(err);
          return { data: [] }; // fallback if workload fails
        }),
        userApi.getFeedbackByMonth(params).catch(() => ({ data: [] })),
      ]);

      setSummaryData(summaryRes.data);
      setStaffData(staffRes.data || []);
      setTrendData(trendRes.data || []);
    } catch (err) {
      setError(err.message || "Lỗi khi tải dữ liệu báo cáo.");
    } finally {
      setLoading(false);
      setFiltering(false);
    }
  }, [appliedFilters]);

  useEffect(() => {
    feedbackApi.getCategories()
      .then((response) => setCategories(response.data || []))
      .catch(() => setCategories([]));
  }, []);

  useEffect(() => {
    fetchReportData();
  }, [fetchReportData]);

  const handleApplyFilters = (e) => {
    e.preventDefault();
    setFiltering(true);
    setAppliedFilters({
      fromDate,
      toDate,
      categoryId: categoryFilter,
    });
  };

  const handleResetFilters = () => {
    setFromDate("");
    setToDate("");
    setCategoryFilter("");
    setFiltering(true);
    setAppliedFilters({ fromDate: "", toDate: "", categoryId: "" });
  };

  const calculatePercentage = (count, total) => {
    if (!total || total <= 0) return 0;
    return ((count / total) * 100).toFixed(1);
  };

  if (loading && !filtering) {
    return (
      <div className="loading-screen">
        <div className="spinner" />
        <p>Đang tổng hợp báo cáo dữ liệu...</p>
      </div>
    );
  }

  return (
    <div className="reports-page">
      <div
        className="page-header flex-between"
        style={{ flexWrap: "wrap", gap: "1rem" }}
      >
        <div>
          <h1>Báo Cáo Thống Kê</h1>
          <p>
            Phân tích hiệu suất giải quyết phản hồi và khối lượng công việc nhân
            viên.
          </p>
        </div>
        <button className="btn btn-secondary btn-sm" onClick={fetchReportData}>
          🔄 Tải lại
        </button>
      </div>

      {error && <div className="alert alert-error mb-4">{error}</div>}

      {/* Filter Section */}
      <form onSubmit={handleApplyFilters} className="card mb-4">
        <div className="card-header">
          <h3 className="card-title">Bộ lọc báo cáo</h3>
        </div>
        <div style={{ padding: "0 1.5rem 1.5rem" }}>
          <div style={{ display: "flex", gap: "1rem", flexWrap: "wrap" }}>
            <div
              className="form-group"
              style={{ flex: 1, minWidth: 150, marginBottom: 0 }}
            >
              <label className="form-label">Từ ngày</label>
              <input
                id="report-from-date"
                type="date"
                className="form-control"
                value={fromDate}
                onChange={(e) => setFromDate(e.target.value)}
              />
            </div>
            <div
              className="form-group"
              style={{ flex: 1, minWidth: 150, marginBottom: 0 }}
            >
              <label className="form-label">Đến ngày</label>
              <input
                id="report-to-date"
                type="date"
                className="form-control"
                value={toDate}
                onChange={(e) => setToDate(e.target.value)}
              />
            </div>
            <div
              className="form-group"
              style={{ flex: 1.5, minWidth: 180, marginBottom: 0 }}
            >
              <label className="form-label">Danh mục</label>
              <select
                id="report-category"
                className="form-control"
                value={categoryFilter}
                onChange={(e) => setCategoryFilter(e.target.value)}
              >
                <option value="">Tất cả danh mục</option>
                {categories.map((category) => (
                  <option key={category.id} value={category.id}>{category.name}</option>
                ))}
              </select>
            </div>
            <div
              style={{
                display: "flex",
                gap: "0.5rem",
                alignItems: "flex-end",
                minWidth: 200,
                flex: 1,
              }}
            >
              <button
                id="btn-apply-report-filters"
                type="submit"
                className="btn btn-primary"
                style={{ flex: 1, height: "45px" }}
                disabled={filtering}
              >
                {filtering ? "Đang lọc..." : "Lọc"}
              </button>
              <button
                id="btn-reset-report-filters"
                type="button"
                className="btn btn-secondary"
                style={{ flex: 1, height: "45px" }}
                onClick={handleResetFilters}
              >
                Đặt lại
              </button>
            </div>
          </div>
        </div>
      </form>

      {summaryData && (
        <>
          {/* Stats Cards */}
          <div className="stat-grid mb-4">
            <div className="stat-card">
              <div className="stat-info">
                <div className="stat-value text-primary">
                  {summaryData.totalFeedbacks ?? 0}
                </div>
                <div className="stat-label">Tổng phản hồi</div>
              </div>
            </div>
            <div className="stat-card">
              <div className="stat-info">
                <div className="stat-value text-warning">
                  {summaryData.openFeedbacks ?? 0}
                </div>
                <div className="stat-label">Đang chờ xử lý</div>
              </div>
            </div>
            <div className="stat-card">
              <div className="stat-info">
                <div className="stat-value text-warning">
                  {summaryData.averageRating
                    ? `${summaryData.averageRating.toFixed(1)}/5`
                    : "0/5"}
                </div>
                <div className="stat-label">Đánh giá trung bình</div>
              </div>
            </div>

            <div className="stat-card">
              <div className="stat-info">
                <div className="stat-value text-success">
                  {summaryData.resolutionRate
                    ? `${summaryData.resolutionRate.toFixed(1)}%`
                    : "0%"}
                </div>
                <div className="stat-label">Tỷ lệ giải quyết</div>
              </div>
            </div>

            <div className="stat-card">
              <div className="stat-info">
                <div className="stat-value text-danger">
                  {summaryData.unresolvedHighPriorityCount ?? 0}
                </div>
                <div className="stat-label">Ưu tiên cao chưa xử lý</div>
              </div>
            </div>
            <div className="stat-card">
              <div className="stat-info">
                <div className="stat-value text-success">
                  {summaryData.resolvedFeedbacks ?? 0}
                </div>
                <div className="stat-label">Đã giải quyết</div>
              </div>
            </div>
            <div className="stat-card">
              <div className="stat-info">
                <div className="stat-value text-muted">
                  {summaryData.closedFeedbacks ?? 0}
                </div>
                <div className="stat-label">Đã đóng</div>
              </div>
            </div>
            <div className="stat-card">
              <div className="stat-info">
                <div className="stat-value">
                  {summaryData.averageResolutionTimeHours
                    ? `${summaryData.averageResolutionTimeHours.toFixed(1)}h`
                    : "0h"}
                </div>
                <div className="stat-label">Thời gian giải quyết TB</div>
              </div>
            </div>
          </div>

          {/* Breakdowns Grid */}
          <div
            style={{
              display: "grid",
              gridTemplateColumns: "repeat(auto-fit, minmax(300px, 1fr))",
              gap: "1.5rem",
              marginBottom: "1.5rem",
            }}
          >
            {/* By Status */}
            <div className="card">
              <div className="card-header">
                <h3 className="card-title">Phân Bố Trạng Thái</h3>
              </div>
              <div
                style={{
                  display: "flex",
                  flexDirection: "column",
                  gap: "1rem",
                }}
              >
                {Object.keys(summaryData.byStatus || {}).length === 0 ? (
                  <p className="text-muted italic py-4 text-center">
                    Chưa có dữ liệu trạng thái.
                  </p>
                ) : (
                  Object.entries(summaryData.byStatus).map(
                    ([status, count]) => {
                      const pct = calculatePercentage(
                        count,
                        summaryData.totalFeedbacks,
                      );
                      return (
                        <div key={status}>
                          <div
                            className="flex-between mb-1"
                            style={{ fontSize: "0.875rem" }}
                          >
                            <span className="font-semibold">{status}</span>
                            <span className="text-muted">
                              {count} phiếu ({pct}%)
                            </span>
                          </div>
                          <div
                            style={{
                              height: "8px",
                              background: "rgba(255,255,255,0.05)",
                              borderRadius: "4px",
                              overflow: "hidden",
                            }}
                          >
                            <div
                              style={{
                                width: `${pct}%`,
                                height: "100%",
                                background: "var(--primary)",
                                borderRadius: "4px",
                              }}
                            />
                          </div>
                        </div>
                      );
                    },
                  )
                )}
              </div>
            </div>

            {/* By Category */}
            <div className="card">
              <div className="card-header">
                <h3 className="card-title">Phân Bố Danh Mục</h3>
              </div>
              <div
                style={{
                  display: "flex",
                  flexDirection: "column",
                  gap: "1rem",
                }}
              >
                {Object.keys(summaryData.byCategory || {}).length === 0 ? (
                  <p className="text-muted italic py-4 text-center">
                    Chưa có dữ liệu danh mục.
                  </p>
                ) : (
                  Object.entries(summaryData.byCategory).map(([cat, count]) => {
                    const pct = calculatePercentage(
                      count,
                      summaryData.totalFeedbacks,
                    );
                    return (
                      <div key={cat}>
                        <div
                          className="flex-between mb-1"
                          style={{ fontSize: "0.875rem" }}
                        >
                          <span className="font-semibold">{cat}</span>
                          <span className="text-muted">
                            {count} phiếu ({pct}%)
                          </span>
                        </div>
                        <div
                          style={{
                            height: "8px",
                            background: "rgba(255,255,255,0.05)",
                            borderRadius: "4px",
                            overflow: "hidden",
                          }}
                        >
                          <div
                            style={{
                              width: `${pct}%`,
                              height: "100%",
                              background: "var(--secondary)",
                              borderRadius: "4px",
                            }}
                          />
                        </div>
                      </div>
                    );
                  })
                )}
              </div>
            </div>

            {/* By Priority */}
            <div className="card">
              <div className="card-header">
                <h3 className="card-title">Phân Bố Mức Độ Ưu Tiên</h3>
              </div>
              <div
                style={{
                  display: "flex",
                  flexDirection: "column",
                  gap: "1rem",
                }}
              >
                {Object.keys(summaryData.byPriority || {}).length === 0 ? (
                  <p className="text-muted italic py-4 text-center">
                    Chưa có dữ liệu ưu tiên.
                  </p>
                ) : (
                  Object.entries(summaryData.byPriority).map(([pri, count]) => {
                    const pct = calculatePercentage(
                      count,
                      summaryData.totalFeedbacks,
                    );
                    return (
                      <div key={pri}>
                        <div
                          className="flex-between mb-1"
                          style={{ fontSize: "0.875rem" }}
                        >
                          <span className="font-semibold">{pri}</span>
                          <span className="text-muted">
                            {count} phiếu ({pct}%)
                          </span>
                        </div>
                        <div
                          style={{
                            height: "8px",
                            background: "rgba(255,255,255,0.05)",
                            borderRadius: "4px",
                            overflow: "hidden",
                          }}
                        >
                          <div
                            style={{
                              width: `${pct}%`,
                              height: "100%",
                              background:
                                pri === "Urgent" || pri === "High"
                                  ? "var(--danger)"
                                  : pri === "Medium"
                                    ? "var(--warning)"
                                    : "var(--info)",
                              borderRadius: "4px",
                            }}
                          />
                        </div>
                      </div>
                    );
                  })
                )}
              </div>
            </div>
          </div>
        </>
      )}

      {/* Staff Performance & Workload */}
      <div className="card mb-4">
        <div className="card-header">
          <h2 className="card-title">Xu hướng phản hồi theo tháng</h2>
          <span className="text-muted" style={{ fontSize: '0.8rem' }}>Tổng số, đã giải quyết và đã đóng</span>
        </div>
        {trendData.length === 0 ? <div className="empty-state"><p>Không có dữ liệu xu hướng trong khoảng đã chọn.</p></div> : (
          <div className="table-wrap"><table><thead><tr><th>Tháng</th><th>Tổng phản hồi</th><th>Đã giải quyết</th><th>Đã đóng</th></tr></thead><tbody>
            {trendData.map((point) => <tr key={point.period}><td className="font-semibold">{point.period}</td><td>{point.totalCount}</td><td>{point.resolvedCount}</td><td>{point.closedCount}</td></tr>)}
          </tbody></table></div>
        )}
      </div>

      {/* Staff Performance & Workload */}
      <div className="card">
        <div className="card-header">
          <h2 className="card-title">📊 Hiệu Suất Nhân Viên Hỗ Trợ</h2>
          <span className="text-muted" style={{ fontSize: "0.8rem" }}>
            Khối lượng và tiến độ công việc
          </span>
        </div>

        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Tên nhân viên</th>
                <th>Tổng số việc đã giao</th>
                <th>Tổng số việc đã giải quyết</th>
                <th>Tỷ lệ giải quyết</th>
                <th>Thời gian giải quyết TB</th>
              </tr>
            </thead>
            <tbody>
              {staffData.length === 0 ? (
                <tr>
                  <td colSpan={5} className="text-center py-4 text-muted">
                    Không có dữ liệu hiệu suất nhân viên.
                  </td>
                </tr>
              ) : (
                staffData.map((staff) => {
                  const rate = calculatePercentage(
                    staff.resolvedCount,
                    staff.assignedCount,
                  );
                  return (
                    <tr key={staff.staffUserId}>
                      <td className="font-semibold">
                        {staff.staffName || "---"}
                      </td>
                      <td>{staff.assignedCount}</td>
                      <td>{staff.resolvedCount}</td>
                      <td>
                        <div
                          style={{
                            display: "flex",
                            alignItems: "center",
                            gap: "0.5rem",
                          }}
                        >
                          <span className="font-semibold">{rate}%</span>
                          <div
                            style={{
                              width: "60px",
                              height: "6px",
                              background: "rgba(255,255,255,0.05)",
                              borderRadius: "3px",
                              overflow: "hidden",
                            }}
                          >
                            <div
                              style={{
                                width: `${rate}%`,
                                height: "100%",
                                background: "var(--success)",
                              }}
                            />
                          </div>
                        </div>
                      </td>
                      <td>
                        {staff.averageResolutionTimeHours
                          ? `${staff.averageResolutionTimeHours.toFixed(1)} giờ`
                          : "---"}
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};

export default ReportsPage;
