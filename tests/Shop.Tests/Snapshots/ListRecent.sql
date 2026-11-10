SELECT "o0"."Id", "c"."Name", "o0"."PlacedAt", (
    SELECT COUNT(*)
    FROM "OrderLines" AS "o1"
    WHERE "o0"."Id" = "o1"."OrderId"), (
    SELECT COALESCE(SUM(CAST("o2"."Quantity" AS INTEGER) * "o2"."UnitPriceCents"), 0)
    FROM "OrderLines" AS "o2"
    WHERE "o0"."Id" = "o2"."OrderId")
FROM (
    SELECT "o"."Id", "o"."CustomerId", "o"."PlacedAt"
    FROM "Orders" AS "o"
    ORDER BY "o"."PlacedAt" DESC, "o"."Id" DESC
    LIMIT @p1 OFFSET @p
) AS "o0"
INNER JOIN "Customers" AS "c" ON "o0"."CustomerId" = "c"."Id"
ORDER BY "o0"."PlacedAt" DESC, "o0"."Id" DESC
